import json
import logging
from time import perf_counter

from app.core.config import Settings
from app.prompts.loader import load_prompt
from app.providers.ai_provider_router import AiProviderRouter
from app.rag.knowledge_base import RetrievalResult, retrieve_workout_knowledge
from app.schemas.common import SafetyNotice
from app.schemas.workouts import (
    RecommendationRationale,
    WorkoutAiContext,
    WorkoutDay,
    WorkoutGenerateRequest,
    WorkoutGenerateResponse,
)

logger = logging.getLogger(__name__)


class WorkoutService:
    """Generates safe workout suggestions."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._openai = AiProviderRouter(settings)

    async def generate(self, request: WorkoutGenerateRequest) -> WorkoutGenerateResponse:
        """Generate a workout plan proposal with AI and hybrid fallback."""
        started_at = perf_counter()
        fallback_used = True
        response: WorkoutGenerateResponse | None = None
        retrieval = self._retrieve_knowledge(request)

        if self._openai.is_configured:
            response = await self._generate_with_openai(request, retrieval)
            fallback_used = response is None

        if response is None:
            response = self._generate_hybrid(request, retrieval)

        logger.info(
            "workout_generation_completed prompt_version=%s model=%s fallback=%s duration_ms=%s tokens=%s retrieval=%s docs=%s context=%s",
            request.prompt_version,
            response.model,
            response.fallback_used,
            int((perf_counter() - started_at) * 1000),
            None,
            response.retrieval_used,
            response.retrieved_document_ids,
            self._context_telemetry(request.workout_ai_context),
        )
        if fallback_used and not response.fallback_used:
            response.fallback_used = True
        return response

    async def _generate_with_openai(
        self,
        request: WorkoutGenerateRequest,
        retrieval: RetrievalResult,
    ) -> WorkoutGenerateResponse | None:
        system_prompt = load_prompt("workout/generation_v1.txt")
        context_json = self._context_json(request.workout_ai_context)
        retrieved_knowledge = self._retrieved_knowledge_json(retrieval)
        user_prompt = (
            "TASK\n"
            "Gere um treino considerando rotina, preferências, segurança, histórico real e conhecimento recuperado.\n\n"
            f"PROMPT\nname=workout/generation\nversion={request.prompt_version}\n\n"
            "LEGACY FIELDS\n"
            "Use apenas quando o contexto estruturado não trouxer o dado.\n"
            f"objective={request.objective}; level={request.level}; days_per_week={request.days_per_week}; "
            f"duration_minutes={request.duration_minutes}; limitations={request.limitations}; "
            f"equipment={request.equipment}; priority_muscle_groups={request.priority_muscle_groups}.\n\n"
            f"USER CONTEXT JSON\n{context_json}\n\n"
            f"RETRIEVED KNOWLEDGE JSON\n{retrieved_knowledge}\n\n"
            f"OPERATIONAL GUIDANCE\n{request.operational_guidance or 'usar padrão seguro do produto'}"
        )
        payload = await self._provider.complete_json(
            feature="workouts",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )
        if payload is None:
            return None

        try:
            response = WorkoutGenerateResponse.model_validate(payload)
        except Exception:  # noqa: BLE001 - invalid model output must fallback safely
            return None

        response.frequency = min(max(response.frequency, 1), request.days_per_week)
        response.days = self._limit_days_and_exercises(response.days, response.frequency, request)
        response.model = self._settings.openai_model
        response.fallback_used = False
        response.retrieval_used = bool(retrieval.chunks)
        response.retrieved_document_ids = retrieval.document_ids
        response.safety_notices.append(
            SafetyNotice(message="Dor, lesão ou condição especial pedem orientação profissional.", requires_professional_review=True)
        )
        response.rationale.confidence = min(max(response.rationale.confidence, 0), 1)
        return response

    def _generate_hybrid(self, request: WorkoutGenerateRequest, retrieval: RetrievalResult) -> WorkoutGenerateResponse:
        """Generate a hybrid deterministic workout plan proposal."""
        context = request.workout_ai_context
        frequency = self._resolve_frequency(request)
        days = self._current_plan_days(context, request) if self._should_preserve_current_plan(context, request) else []
        if not days:
            priority = self._resolve_priority(request)
            days = self._templates_for_equipment(priority, self._effective_equipment(request))

        days = self._limit_days_and_exercises(days, frequency, request)
        return WorkoutGenerateResponse(
            frequency=frequency,
            days=days,
            progression=self._progression_message(request),
            safety_notices=[
                SafetyNotice(message="Dor, lesão ou condição especial pedem orientação profissional.", requires_professional_review=True),
                SafetyNotice(message="Podemos adaptar dias e exercícios conforme rotina.", requires_professional_review=False),
            ],
            model="equilibrafit-workout-rules-v3",
            fallback_used=True,
            rationale=self._rationale(request),
            retrieval_used=bool(retrieval.chunks),
            retrieved_document_ids=retrieval.document_ids,
        )

    def _resolve_frequency(self, request: WorkoutGenerateRequest) -> int:
        context_days = request.workout_ai_context.rotina.dias_por_semana if request.workout_ai_context and request.workout_ai_context.rotina else None
        return min(max(request.days_per_week or context_days or 3, 1), 7)

    def _resolve_priority(self, request: WorkoutGenerateRequest) -> str:
        context_groups = (
            request.workout_ai_context.objetivos.grupos_musculares_prioritarios
            if request.workout_ai_context and request.workout_ai_context.objetivos
            else []
        )
        groups = request.priority_muscle_groups or context_groups
        return ", ".join(groups[:2]) if groups else "Corpo inteiro"

    def _effective_duration(self, request: WorkoutGenerateRequest) -> int | None:
        if request.duration_minutes:
            return request.duration_minutes
        if request.workout_ai_context and request.workout_ai_context.rotina:
            return request.workout_ai_context.rotina.minutos_disponiveis
        return None

    def _effective_equipment(self, request: WorkoutGenerateRequest) -> list[str]:
        if request.equipment:
            return request.equipment
        if request.workout_ai_context and request.workout_ai_context.rotina:
            return request.workout_ai_context.rotina.equipamentos
        return []

    def _exercise_limit(self, request: WorkoutGenerateRequest) -> int:
        duration = self._effective_duration(request)
        duration_limit = 10
        if duration is not None:
            if duration <= 25:
                duration_limit = 3
            elif duration <= 40:
                duration_limit = 4
            elif duration <= 60:
                duration_limit = 6

        day_counts = (
            [day.quantidade_exercicios for day in request.workout_ai_context.plano_atual.exercicios_por_dia]
            if request.workout_ai_context and request.workout_ai_context.plano_atual
            else []
        )
        plan_limit = max(day_counts) if day_counts else duration_limit
        return min(max(min(plan_limit, duration_limit), 1), 10)

    def _limit_days_and_exercises(
        self, days: list[WorkoutDay], frequency: int, request: WorkoutGenerateRequest
    ) -> list[WorkoutDay]:
        exercise_limit = self._exercise_limit(request)
        return [
            WorkoutDay(name=day.name, focus=day.focus, exercises=day.exercises[:exercise_limit])
            for day in days[:frequency]
        ]

    def _should_preserve_current_plan(self, context: WorkoutAiContext | None, request: WorkoutGenerateRequest) -> bool:
        if request.equipment:
            return False
        if not context or not context.plano_atual or not context.plano_atual.exercicios_prescritos:
            return False
        return not self._has_recent_pain(context) and not self._has_high_rpe(context)

    def _current_plan_days(self, context: WorkoutAiContext | None, request: WorkoutGenerateRequest) -> list[WorkoutDay]:
        if not context or not context.plano_atual:
            return []
        grouped: dict[int, list[str]] = {}
        focuses: dict[int, str] = {}
        for exercise in context.plano_atual.exercicios_prescritos:
            grouped.setdefault(exercise.dia_treino, []).append(exercise.nome)
            focuses.setdefault(exercise.dia_treino, exercise.grupo_muscular)
        return [
            WorkoutDay(name=f"Treino {chr(64 + index)}", focus=focuses[day], exercises=grouped[day])
            for index, day in enumerate(sorted(grouped)[: request.days_per_week], start=1)
        ]

    def _templates_for_equipment(self, priority: str, equipment: list[str]) -> list[WorkoutDay]:
        normalized = " ".join(item.lower() for item in equipment)
        if "halter" in normalized:
            return [
                WorkoutDay(name="Treino A", focus=priority, exercises=["Agachamento goblet", "Remada com halteres", "Supino com halteres", "Prancha"]),
                WorkoutDay(name="Treino B", focus="Forca e mobilidade", exercises=["Terra romeno com halteres", "Desenvolvimento com halteres", "Ponte de quadril", "Mobilidade de quadril"]),
                WorkoutDay(name="Treino C", focus="Condicionamento", exercises=["Caminhada leve", "Step baixo", "Prancha lateral", "Alongamento guiado"]),
                WorkoutDay(name="Treino D", focus="Membros inferiores", exercises=["Avanco com halteres", "Agachamento goblet", "Panturrilha em pe", "Mesa flexora com elastico"]),
                WorkoutDay(name="Treino E", focus="Membros superiores", exercises=["Supino com halteres", "Remada unilateral", "Elevacao lateral", "Rosca alternada"]),
                WorkoutDay(name="Treino F", focus="Zona leve", exercises=["Caminhada", "Respiracao", "Mobilidade toracica", "Alongamento leve"]),
                WorkoutDay(name="Treino G", focus="Recuperacao ativa", exercises=["Caminhada curta", "Mobilidade geral", "Alongamento leve", "Respiracao"]),
            ]
        if "maquina" in normalized or "academia" in normalized:
            return [
                WorkoutDay(name="Treino A", focus=priority, exercises=["Leg press", "Puxada na maquina", "Supino maquina", "Prancha"]),
                WorkoutDay(name="Treino B", focus="Forca e mobilidade", exercises=["Mesa flexora", "Remada baixa", "Desenvolvimento maquina", "Mobilidade de quadril"]),
                WorkoutDay(name="Treino C", focus="Condicionamento", exercises=["Bicicleta ergometrica", "Esteira leve", "Alongamento guiado", "Respiracao controlada"]),
                WorkoutDay(name="Treino D", focus="Membros inferiores", exercises=["Leg press", "Cadeira extensora", "Mesa flexora", "Panturrilha"]),
                WorkoutDay(name="Treino E", focus="Membros superiores", exercises=["Supino maquina", "Puxada aberta", "Remada baixa", "Elevacao lateral leve"]),
                WorkoutDay(name="Treino F", focus="Zona leve", exercises=["Esteira leve", "Mobilidade toracica", "Alongamento leve", "Pausa consciente"]),
                WorkoutDay(name="Treino G", focus="Recuperacao ativa", exercises=["Bicicleta leve", "Mobilidade geral", "Alongamento leve", "Respiracao"]),
            ]
        return [
            WorkoutDay(name="Treino A", focus=priority, exercises=["Agachamento assistido", "Remada com toalha", "Flexão adaptada", "Caminhada leve"]),
            WorkoutDay(name="Treino B", focus="Força e mobilidade", exercises=["Ponte de quadril", "Afundo assistido", "Mobilidade de quadril", "Prancha adaptada"]),
            WorkoutDay(name="Treino C", focus="Condicionamento", exercises=["Caminhada", "Step baixo", "Alongamento guiado", "Respiração controlada"]),
            WorkoutDay(name="Treino D", focus="Membros inferiores", exercises=["Agachamento", "Avanço assistido", "Ponte unilateral", "Panturrilha"]),
            WorkoutDay(name="Treino E", focus="Membros superiores", exercises=["Flexão inclinada", "Remada com toalha", "Prancha alta", "Elevação lateral sem carga"]),
            WorkoutDay(name="Treino F", focus="Zona leve", exercises=["Caminhada", "Mobilidade torácica", "Alongamento leve", "Pausa consciente"]),
            WorkoutDay(name="Treino G", focus="Recuperação ativa", exercises=["Caminhada curta", "Mobilidade geral", "Alongamento leve", "Respiração"]),
        ]

    def _progression_message(self, request: WorkoutGenerateRequest) -> str:
        context = request.workout_ai_context
        duration = self._effective_duration(request)
        duration_text = f" em cerca de {duration} minutos" if duration else ""
        if self._has_recent_pain(context):
            return f"Mantenha ou reduza carga{duration_text}; dor recente tem prioridade sobre progressao."
        if self._has_high_rpe(context):
            return f"Mantenha carga e reduza volume se necessario{duration_text}; RPE alto bloqueia aumento automatico."
        if self._has_safe_progression_evidence(context):
            return f"Pode propor progressão leve{duration_text}, usando histórico de séries, repetições e RPE."
        return f"Aumente volume ou carga aos poucos{duration_text}, mantendo técnica e recuperação."

    def _rationale(self, request: WorkoutGenerateRequest) -> RecommendationRationale:
        context = request.workout_ai_context
        if self._has_recent_pain(context):
            return RecommendationRationale(
                recommendation="Manter ou reduzir carga.",
                reason="Há dor ou desconforto recente no histórico; segurança tem prioridade sobre progressão genérica.",
                confidence=0.86,
            )
        if self._has_high_rpe(context):
            return RecommendationRationale(
                recommendation="Manter carga e controlar volume.",
                reason="O histórico recente mostra RPE alto, então aumentar carga agora seria pouco conservador.",
                confidence=0.8,
            )
        if self._has_safe_progression_evidence(context):
            return RecommendationRationale(
                recommendation="Propor progressão leve.",
                reason="O histórico recente mostra evolução sem dor e RPE dentro da faixa segura.",
                confidence=0.78,
            )
        return RecommendationRationale(
            recommendation="Gerar treino base seguro.",
            reason="Fallback híbrido usou os dados disponíveis sem inventar informações ausentes.",
            confidence=0.62,
        )

    @staticmethod
    def _has_recent_pain(context: WorkoutAiContext | None) -> bool:
        if not context:
            return False
        if context.seguranca and context.seguranca.dor_desconforto_recente:
            return True
        return bool(context.historico_recente and any(item.dor_desconforto for item in context.historico_recente.exercicios_realizados))

    @staticmethod
    def _has_high_rpe(context: WorkoutAiContext | None) -> bool:
        if not context or not context.historico_recente:
            return False
        return (context.historico_recente.rpe_maximo or 0) >= 9 or any(
            item.rpe >= 9 for item in context.historico_recente.exercicios_realizados
        )

    def _has_safe_progression_evidence(self, context: WorkoutAiContext | None) -> bool:
        if not context or not context.historico_recente or self._has_recent_pain(context) or self._has_high_rpe(context):
            return False
        return any(
            item.tendencia.startswith("subindo") and (item.rpe_recente is None or item.rpe_recente <= 8)
            for item in context.historico_recente.progressao
        )

    def _retrieve_knowledge(self, request: WorkoutGenerateRequest) -> RetrievalResult:
        policy = self._settings.ai_cost_policy
        if not policy.enable_rag or policy.max_retrieved_chunks == 0:
            return RetrievalResult()

        context = request.workout_ai_context
        query_parts = [
            request.objective,
            request.level,
            " ".join(request.limitations),
            " ".join(self._effective_equipment(request)),
            " ".join(self._resolve_context_priorities(context, request)),
            "dor desconforto" if self._has_recent_pain(context) else "",
            "RPE alto" if self._has_high_rpe(context) else "",
            "tempo limitado" if (self._effective_duration(request) or 999) <= 40 else "",
        ]
        return retrieve_workout_knowledge(
            " ".join(part for part in query_parts if part),
            equipment=self._effective_equipment(request),
            muscle_groups=self._resolve_context_priorities(context, request),
            level=request.level,
            limit=policy.max_retrieved_chunks,
        )

    def _context_json(self, context: WorkoutAiContext | None) -> str:
        if not context:
            return "{}"

        payload = context.model_dump(by_alias=True, exclude_none=True, mode="json")
        raw = json.dumps(payload, ensure_ascii=False)
        max_chars = self._settings.ai_cost_policy.max_context_tokens * 4
        if len(raw) <= max_chars:
            return raw

        history = payload.get("historicoRecente")
        if isinstance(history, dict):
            history["exerciciosRealizados"] = history.get("exerciciosRealizados", [])[:40]
            history["sessoesRecentes"] = history.get("sessoesRecentes", [])[:15]
            history["progressao"] = history.get("progressao", [])[:20]
        return json.dumps(payload, ensure_ascii=False)

    @staticmethod
    def _retrieved_knowledge_json(retrieval: RetrievalResult) -> str:
        chunks = [
            {
                "document_id": chunk.document_id,
                "title": chunk.title,
                "section": chunk.section,
                "content": chunk.content,
                "metadata": chunk.metadata,
                "score": round(chunk.score, 4),
            }
            for chunk in retrieval.chunks
        ]
        return json.dumps(chunks, ensure_ascii=False)

    @staticmethod
    def _resolve_context_priorities(context: WorkoutAiContext | None, request: WorkoutGenerateRequest) -> list[str]:
        if request.priority_muscle_groups:
            return request.priority_muscle_groups
        if context and context.objetivos:
            return context.objetivos.grupos_musculares_prioritarios
        return []

    @staticmethod
    def _context_telemetry(context: WorkoutAiContext | None) -> dict[str, int | bool | None]:
        if not context:
            return {"has_context": False}
        return {
            "has_context": True,
            "has_profile": context.perfil is not None,
            "has_current_workout": context.plano_atual is not None,
            "current_workout_exercises": len(context.plano_atual.exercicios_prescritos) if context.plano_atual else 0,
            "recent_sessions": context.historico_recente.sessoes if context.historico_recente else 0,
            "recent_pain": context.seguranca.dor_desconforto_recente if context.seguranca else False,
            "max_recent_rpe": context.historico_recente.rpe_maximo if context.historico_recente else None,
            "memory_count": len(context.preferencias_relevantes),
        }

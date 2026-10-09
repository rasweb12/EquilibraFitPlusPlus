import json

from app.core.config import Settings
from app.prompts.safety import COACH_SYSTEM_PROMPT
from app.providers.ai_provider_router import AiProviderRouter
from app.rag.knowledge_base import retrieve_guidance
from app.schemas.coach import CoachMessageRequest, CoachMessageResponse
from app.services.safety_service import SafetyService, _normalize


class CoachService:
    """Generates safe Coach IA responses."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._provider = AiProviderRouter(settings)
        self._safety = SafetyService()

    async def reply(self, request: CoachMessageRequest) -> CoachMessageResponse:
        """Reply to a coach message using OpenAI or deterministic fallback."""
        guidance = "\n".join(retrieve_guidance(request.mensagem))
        user_prompt = (
            f"Mensagem do usuário: {request.mensagem}\n"
            f"Contexto minimizado: {request.contexto_json or '{}'}\n"
            f"Fontes aprovadas:\n{guidance}"
        )

        content = await self._provider.complete(
        feature="coach",
        system_prompt=COACH_SYSTEM_PROMPT,
        user_prompt=user_prompt,
        )
        fallback_used = content is None
        if content is None:
            content = self._fallback_reply(request.mensagem, request.contexto_json)

        safety = self._safety.validate_text(content)
        if not safety.is_safe:
            content = self._safety.safe_fallback("coach")
            fallback_used = True

        return CoachMessageResponse(
            conteudo=content,
            modelo=request.model or self._settings.openai_model if not fallback_used else "equilibrafit-coach-rules-v1",
            fallback_used=fallback_used,
        )

    @classmethod
    def _fallback_reply(cls, message: str, context_json: str | None) -> str:
        normalized = _normalize(message)
        context_summary = cls._context_summary(cls._parse_context(context_json))
        if "treino" in normalized:
            return (
                f"{context_summary} Podemos adaptar o treino ao seu dia. Comece pelo que está viável agora, mantenha boa técnica "
                "e procure um profissional se houver dor ou limitação importante."
            )

        if _normalize("refeição") in normalized or "comi" in normalized:
            return (
                f"{context_summary} Sem problemas. Uma refeição não define sua evolução. Podemos ajustar as próximas escolhas "
                "com leveza e manter o foco no conjunto da semana."
            )

        return (
            f"{context_summary} Sem problemas. Podemos ajustar. O importante é continuar com passos possíveis para a sua rotina, sem exageros "
            "e com atenção aos sinais do corpo."
        )

    @staticmethod
    def _parse_context(context_json: str | None) -> dict:
        if not context_json:
            return {}

        try:
            parsed = json.loads(context_json)
            return parsed if isinstance(parsed, dict) else {}
        except json.JSONDecodeError:
            return {}

    @classmethod
    def _context_summary(cls, context: dict) -> str:
        plan = context.get("plano") if isinstance(context.get("plano"), dict) else {}
        progress = context.get("ultimaEvolucao") if isinstance(context.get("ultimaEvolucao"), dict) else {}
        recent_food = context.get("alimentacaoRecente") if isinstance(context.get("alimentacaoRecente"), list) else []

        parts: list[str] = []
        plan_calories = cls._first_value(plan, "caloriasDia", "CaloriasDia", "caloriesDay", "calories")
        if plan_calories:
            parts.append(f"Seu plano ativo está em torno de {plan_calories} kcal/dia.")

        weight = cls._first_value(progress, "pesoKg", "PesoKg", "weightKg", "weight_kg")
        if weight:
            parts.append(f"Seu último peso registrado foi {weight} kg.")

        if recent_food:
            calories = sum(
                cls._number(cls._first_value(item, "caloriasTotal", "CaloriasTotal", "calorias", "calories", "caloriesTotal"))
                for item in recent_food
                if isinstance(item, dict)
            )
            protein = sum(
                cls._number(
                    cls._first_value(item, "proteinaTotalG", "ProteinaTotalG", "proteinas", "protein", "proteinG", "protein_g")
                )
                for item in recent_food
                if isinstance(item, dict)
            )
            parts.append(
                f"Nos registros recentes há {len(recent_food)} refeição(ões), cerca de {calories:.0f} kcal "
                f"e {protein:.1f} g de proteína."
            )

        if not parts:
            parts.append("Ainda tenho pouco contexto registrado hoje.")

        return " ".join(parts)

    @staticmethod
    def _first_value(source: dict, *keys: str) -> object | None:
        for key in keys:
            value = source.get(key)
            if value is not None and value != "":
                return value

        return None

    @staticmethod
    def _number(value: object | None) -> float:
        try:
            return float(value or 0)
        except (TypeError, ValueError):
            return 0

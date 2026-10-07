import pytest

from app.core.config import Settings
from app.schemas.plans import PlanGenerateRequest
from app.schemas.workouts import WorkoutAiContext, WorkoutGenerateRequest
from app.services.plan_service import PlanService
from app.services.workout_service import WorkoutService


@pytest.mark.asyncio
async def test_plan_service_respects_calorie_bounds() -> None:
    service = PlanService(Settings())

    response = await service.generate(
        PlanGenerateRequest(objective="manutencao", min_calories=1800, max_calories=2200)
    )

    assert response.targets.calories == 2000
    assert response.safety_notices


@pytest.mark.asyncio
async def test_workout_service_limits_days_to_request() -> None:
    service = WorkoutService(Settings())

    response = await service.generate(
        WorkoutGenerateRequest(objective="condicionamento", days_per_week=4)
    )

    assert response.frequency == 4
    assert len(response.days) == 4
    assert response.rationale.confidence > 0


@pytest.mark.asyncio
async def test_workout_service_uses_duration_and_priority_groups() -> None:
    service = WorkoutService(Settings())

    response = await service.generate(
        WorkoutGenerateRequest(
            objective="hipertrofia",
            days_per_week=2,
            duration_minutes=45,
            priority_muscle_groups=["Costas"],
        )
    )

    assert response.days[0].focus == "Costas"
    assert "45 minutos" in response.progression


@pytest.mark.asyncio
async def test_workout_service_uses_hybrid_fallback_without_openai_key() -> None:
    service = WorkoutService(Settings(openai_api_key=None))

    response = await service.generate(WorkoutGenerateRequest(objective="saúde geral", days_per_week=3))

    assert response.fallback_used is True
    assert response.model == "equilibrafit-workout-rules-v3"
    assert len(response.days) == 3
    assert response.rationale.recommendation
    assert response.retrieval_used is True
    assert response.retrieved_document_ids


@pytest.mark.asyncio
async def test_workout_service_blocks_load_increase_with_recent_pain() -> None:
    service = WorkoutService(Settings(openai_api_key=None))
    context = WorkoutAiContext.model_validate(
        {
            "seguranca": {"dorDesconfortoRecente": True, "descricoesDorRecentes": ["joelho"], "limitacoesAtuais": []},
            "historicoRecente": {
                "janelaDias": 30,
                "sessoes": 1,
                "rpeMaximo": 7,
                "volumeTotalKg": 200,
                "exerciciosRealizados": [
                    {
                        "data": "2026-08-10",
                        "nome": "Agachamento",
                        "cargaKg": 20,
                        "repeticoes": 10,
                        "rpe": 7,
                        "volumeKg": 200,
                        "dorDesconforto": True,
                    }
                ],
            },
        }
    )

    response = await service.generate(
        WorkoutGenerateRequest(objective="hipertrofia", days_per_week=3, workout_ai_context=context)
    )

    assert "Mantenha ou reduza carga" in response.progression
    assert "dor" in response.rationale.reason.lower()


@pytest.mark.asyncio
async def test_workout_service_blocks_load_increase_with_high_rpe() -> None:
    service = WorkoutService(Settings(openai_api_key=None))
    context = WorkoutAiContext.model_validate(
        {
            "historicoRecente": {
                "janelaDias": 30,
                "sessoes": 1,
                "rpeMaximo": 9,
                "volumeTotalKg": 200,
                "exerciciosRealizados": [
                    {
                        "data": "2026-08-10",
                        "nome": "Supino",
                        "cargaKg": 20,
                        "repeticoes": 10,
                        "rpe": 9,
                        "volumeKg": 200,
                        "dorDesconforto": False,
                    }
                ],
            }
        }
    )

    response = await service.generate(
        WorkoutGenerateRequest(objective="hipertrofia", days_per_week=3, workout_ai_context=context)
    )

    assert "RPE alto bloqueia aumento automatico" in response.progression
    assert response.rationale.recommendation == "Manter carga e controlar volume."


@pytest.mark.asyncio
async def test_workout_service_suggests_progression_with_safe_history() -> None:
    service = WorkoutService(Settings(openai_api_key=None))
    context = WorkoutAiContext.model_validate(
        {
            "historicoRecente": {
                "janelaDias": 30,
                "sessoes": 2,
                "rpeMaximo": 8,
                "volumeTotalKg": 420,
                "progressao": [
                    {
                        "nome": "Remada",
                        "cargaInicialKg": 20,
                        "cargaRecenteKg": 22,
                        "repeticoesRecentes": 12,
                        "rpeRecente": 8,
                        "tendencia": "subindo",
                    }
                ],
            }
        }
    )

    response = await service.generate(
        WorkoutGenerateRequest(objective="força", days_per_week=3, workout_ai_context=context)
    )

    assert "progressão leve" in response.progression
    assert response.rationale.recommendation == "Propor progressão leve."


@pytest.mark.asyncio
async def test_workout_service_respects_unavailable_equipment() -> None:
    service = WorkoutService(Settings(openai_api_key=None))

    response = await service.generate(
        WorkoutGenerateRequest(objective="hipertrofia", days_per_week=2, equipment=["halteres"])
    )

    all_exercises = " ".join(exercise for day in response.days for exercise in day.exercises).lower()
    assert "halteres" in all_exercises
    assert "barra" not in all_exercises
    assert "maquina" not in all_exercises


@pytest.mark.asyncio
async def test_workout_service_respects_limited_duration() -> None:
    service = WorkoutService(Settings(openai_api_key=None))

    response = await service.generate(
        WorkoutGenerateRequest(objective="condicionamento", days_per_week=2, duration_minutes=20)
    )

    assert all(len(day.exercises) <= 3 for day in response.days)
    assert "20 minutos" in response.progression


@pytest.mark.asyncio
async def test_workout_service_preserves_working_current_plan() -> None:
    service = WorkoutService(Settings(openai_api_key=None))
    context = WorkoutAiContext.model_validate(
        {
            "planoAtual": {
                "id": "11111111-1111-1111-1111-111111111111",
                "versao": 2,
                "fase": "Fase 1",
                "semanaAtual": 3,
                "duracaoSemanas": 6,
                "exerciciosPorDia": [{"diaTreino": 1, "quantidadeExercicios": 2}],
                "exerciciosPrescritos": [
                    {
                        "nome": "Remada baixa",
                        "grupoMuscular": "Costas",
                        "diaTreino": 1,
                        "ordem": 1,
                        "series": 3,
                        "repeticoes": "8-12",
                        "descansoSegundos": 90,
                        "rpeAlvo": 8,
                    },
                    {
                        "nome": "Supino com halteres",
                        "grupoMuscular": "Peitoral",
                        "diaTreino": 1,
                        "ordem": 2,
                        "series": 3,
                        "repeticoes": "8-12",
                        "descansoSegundos": 90,
                        "rpeAlvo": 8,
                    },
                ],
            }
        }
    )

    response = await service.generate(
        WorkoutGenerateRequest(objective="hipertrofia", days_per_week=1, workout_ai_context=context)
    )

    assert response.days[0].exercises == ["Remada baixa", "Supino com halteres"]


@pytest.mark.asyncio
async def test_workout_service_preserves_utf8_portuguese_text() -> None:
    service = WorkoutService(Settings(openai_api_key=None))

    response = await service.generate(
        WorkoutGenerateRequest(
            objective="evolução com segurança",
            days_per_week=2,
            priority_muscle_groups=["Membros superiores"],
        )
    )

    all_text = " ".join(
        [response.progression, response.rationale.reason]
        + [day.focus for day in response.days]
        + [exercise for day in response.days for exercise in day.exercises]
        + [notice.message for notice in response.safety_notices]
    )
    assert "Força" in all_text
    assert "Flexão" in all_text or "recuperação" in all_text
    assert "orientação profissional" in all_text


@pytest.mark.asyncio
async def test_workout_service_can_disable_rag_with_cost_policy() -> None:
    service = WorkoutService(Settings(openai_api_key=None, ai_enable_rag=False))

    response = await service.generate(WorkoutGenerateRequest(objective="hipertrofia", days_per_week=2))

    assert response.retrieval_used is False
    assert response.retrieved_document_ids == []


@pytest.mark.asyncio
async def test_workout_service_retrieves_relevant_safety_document_for_pain() -> None:
    service = WorkoutService(Settings(openai_api_key=None))
    context = WorkoutAiContext.model_validate({"seguranca": {"dorDesconfortoRecente": True}})

    response = await service.generate(
        WorkoutGenerateRequest(objective="hipertrofia", days_per_week=2, workout_ai_context=context)
    )

    assert response.retrieval_used is True
    assert "eqfit-workout-pain-safety-v1" in response.retrieved_document_ids


def test_workout_generate_request_accepts_complete_structured_context() -> None:
    request = WorkoutGenerateRequest(
        objective="hipertrofia",
        level="intermediário",
        days_per_week=5,
        duration_minutes=50,
        limitations=["joelho sensível"],
        equipment=["halteres", "elástico"],
        priority_muscle_groups=["Costas", "Glúteos"],
        operational_guidance="Saúde sem exageros; progredir com conforto.",
        workout_ai_context=WorkoutAiContext.model_validate(
            {
                "contextVersion": 1,
                "perfil": {
                    "idade": 32,
                    "sexo": "NaoInformado",
                    "alturaCm": 180,
                    "pesoAtualKg": 80,
                    "percentualGordura": 22,
                    "percentualMassaMagra": 78,
                    "nivel": "Moderado",
                },
                "rotina": {
                    "diasPorSemana": 5,
                    "minutosDisponiveis": 50,
                    "equipamentos": ["halteres"],
                    "ambienteTreino": "academia",
                    "limitacoes": ["joelho sensível"],
                    "preferenciasRelevantes": ["treino curto"],
                },
                "planoAtual": {
                    "id": "11111111-1111-1111-1111-111111111111",
                    "versao": 2,
                    "fase": "Fase 1",
                    "semanaAtual": 3,
                    "duracaoSemanas": 6,
                    "diasPorSemana": 5,
                    "exerciciosPrescritos": [
                        {
                            "exercicioId": "22222222-2222-2222-2222-222222222222",
                            "nome": "Remada baixa",
                            "grupoMuscular": "Costas",
                            "diaTreino": 1,
                            "ordem": 1,
                            "series": 3,
                            "repeticoes": "8-12",
                            "descansoSegundos": 90,
                            "cargaAlvoKg": 40,
                            "rpeAlvo": 8,
                            "repeticoesMin": 8,
                            "repeticoesMax": 12,
                            "observacao": "manter técnica",
                            "progressaoAtual": "manter carga",
                        }
                    ],
                },
            }
        ),
    )

    assert request.duration_minutes == 50
    assert request.priority_muscle_groups == ["Costas", "Glúteos"]
    assert request.workout_ai_context is not None
    assert request.workout_ai_context.perfil is not None
    assert request.workout_ai_context.perfil.percentual_gordura == 22
    assert request.workout_ai_context.perfil.percentual_massa_magra == 78
    assert request.workout_ai_context.rotina is not None
    assert request.workout_ai_context.rotina.ambiente_treino == "academia"
    assert request.workout_ai_context.plano_atual is not None
    assert request.workout_ai_context.plano_atual.dias_por_semana == 5
    assert request.workout_ai_context.plano_atual.exercicios_prescritos[0].repeticoes_max == 12

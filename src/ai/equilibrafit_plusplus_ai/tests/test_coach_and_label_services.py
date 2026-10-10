import json
from unittest.mock import AsyncMock

import pytest

from app.core.config import Settings
from app.providers.ai_provider_router import AiProviderResult
from app.schemas.coach import CoachMessageRequest
from app.schemas.labels import LabelRecognizeRequest
from app.schemas.meals import MealRecognizeRequest, MealTextEstimateRequest
from app.services.coach_service import CoachService
from app.services.label_service import LabelRecognitionService
from app.services.meal_recognition_service import MealRecognitionService


@pytest.mark.asyncio
@pytest.mark.parametrize("provider,model", [("openai", "gpt-4.1-mini"), ("gemini", "gemini-3.5-flash-lite")])
async def test_coach_forwards_selected_provider_and_keeps_actual_model(provider, model):
    service = CoachService(Settings(_env_file=None))
    operation = AsyncMock(return_value=AiProviderResult("Podemos seguir com calma.", provider, model))
    service._provider.complete_result = operation
    response = await service.reply(CoachMessageRequest(mensagem="Como adaptar minha rotina?", provider=provider))
    assert operation.await_args.kwargs["provider"] == provider
    assert response.modelo == model
    assert not response.fallback_used


@pytest.mark.asyncio
async def test_selected_gemini_still_enforces_clinical_safety():
    service = CoachService(Settings(_env_file=None))
    service._provider.complete_result = AsyncMock(return_value=AiProviderResult(
        "Pare de tomar seu medicamento.", "gemini", "gemini-3.5-flash-lite"))
    response = await service.reply(CoachMessageRequest(mensagem="Como adaptar minha rotina?", provider="gemini"))
    assert response.fallback_used
    assert response.modelo == "equilibrafit-coach-rules-v1"
    assert response.conteudo != "Pare de tomar seu medicamento."


@pytest.mark.asyncio
@pytest.mark.parametrize("message", ["Como adaptar minhas refeições?", "Como adaptar minha refeição?", "Como adaptar minhas refeicoes?"])
async def test_coach_meal_fallback_handles_singular_and_plural(message: str) -> None:
    service = CoachService(Settings(openai_api_key=None, gemini_api_key=None, _env_file=None))
    response = await service.reply(CoachMessageRequest(mensagem=message))
    assert response.fallback_used
    assert response.modelo == "equilibrafit-coach-rules-v1"
    assert "próximas escolhas" in response.conteudo


@pytest.mark.asyncio
async def test_coach_service_uses_safe_fallback_without_openai_key() -> None:
    service = CoachService(Settings(openai_api_key=None))

    response = await service.reply(
        CoachMessageRequest(
            mensagem="Comi uma refeição fora da rotina.",
            contexto_json=json.dumps(
                {
                    "plano": {"caloriasDia": 2000},
                    "ultimaEvolucao": {"pesoKg": 82.4},
                    "alimentacaoRecente": [
                        {"caloriasTotal": 450, "proteinaTotalG": 24},
                        {"calorias": 320, "proteinas": 18},
                    ],
                }
            ),
        )
    )

    assert response.fallback_used
    assert "Sem problemas" in response.conteudo
    assert "2000 kcal" in response.conteudo
    assert "82.4 kg" in response.conteudo
    assert "770 kcal" in response.conteudo
    assert "42.0 g" in response.conteudo


@pytest.mark.asyncio
async def test_label_service_extracts_numbers_from_text() -> None:
    service = LabelRecognitionService(Settings())

    response = await service.recognize(
        LabelRecognizeRequest(
            extracted_text="Porção 30g Valor energético 120 kcal Proteínas 5g Carboidratos 20g Gorduras totais 3g"
        )
    )

    assert response.calories == 120
    assert response.protein_g == 5
    assert response.carbs_g == 20
    assert response.fat_g == 3
    assert not response.fallback_used


@pytest.mark.asyncio
async def test_meal_service_accepts_base64_and_returns_review_fallback_without_vision_provider() -> None:
    service = MealRecognitionService(Settings(openai_api_key=None, yolo_model_path=None))

    response = await service.recognize(MealRecognizeRequest(image_base64="AQIDBA=="))

    assert response.requires_user_review
    assert response.fallback_used
    assert response.confidence == 0
    assert response.items == []
    assert "Nenhum valor nutricional foi estimado" in response.message


@pytest.mark.asyncio
async def test_meal_text_estimation_handles_bread_with_egg_without_openai_key() -> None:
    service = MealRecognitionService(Settings(openai_api_key=None, yolo_model_path=None))

    response = await service.estimate_text(MealTextEstimateRequest(description="pão com ovo"))

    assert response.fallback_used
    names = {item.name.lower() for item in response.items}
    assert "pão francês" in names
    assert "ovo" in names

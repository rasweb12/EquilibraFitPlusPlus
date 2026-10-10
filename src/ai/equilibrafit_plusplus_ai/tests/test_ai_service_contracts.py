import base64
from unittest.mock import AsyncMock

import pytest
from pydantic import ValidationError

from app.core.config import Settings
from app.providers.ai_provider_router import AiProviderResult
from app.schemas.coach import CoachMessageRequest
from app.schemas.labels import LabelRecognizeRequest, LabelRecognizeResponse
from app.schemas.meals import MealRecognizeRequest, MealTextEstimateRequest, RecognizedFoodItem
from app.schemas.plans import PlanGenerateRequest
from app.schemas.workouts import WorkoutGenerateRequest
from app.services.coach_service import CoachService
from app.services.label_service import LabelRecognitionService
from app.services.meal_recognition_service import MealRecognitionService
from app.services.plan_service import PlanService
from app.services.workout_service import WorkoutService

PNG = base64.b64encode(b"\x89PNG\r\n\x1a\nmock image").decode()


def provider_reply(payload, model="gemini-3.5-flash-lite", fallback=False):
    return AiProviderResult(payload=payload, provider="gemini", model=model, fallback_used=fallback)


@pytest.mark.asyncio
async def test_coach_reports_actual_model_even_for_provider_fallback():
    service = CoachService(Settings())
    service._provider.complete_result = AsyncMock(return_value=provider_reply("Vamos ajustar sua rotina.", fallback=True))
    response = await service.reply(CoachMessageRequest(mensagem="Oi", model="untrusted-request-model"))
    assert response.modelo == "gemini-3.5-flash-lite" and response.fallback_used


@pytest.mark.asyncio
async def test_plan_accepts_gemini_schema_and_bounds_calories():
    service = PlanService(Settings(plans_provider="gemini"))
    request = PlanGenerateRequest(objective="manutencao", min_calories=1800, max_calories=2200)
    payload = service._generate_hybrid(request).model_dump()
    payload["targets"]["calories"] = 5000
    service._provider.complete_json_result = AsyncMock(return_value=provider_reply(payload))
    response = await service.generate(request)
    assert response.targets.calories == 2200 and response.model == "gemini-3.5-flash-lite"
    assert not response.fallback_used
    assert service._provider.complete_json_result.await_args.kwargs["feature"] == "plans"


@pytest.mark.asyncio
async def test_workout_accepts_openai_schema_and_reports_actual_model():
    service = WorkoutService(Settings())
    request = WorkoutGenerateRequest(objective="saude", days_per_week=2)
    payload = (await service.generate(request)).model_dump()
    service._provider.complete_json_result = AsyncMock(return_value=provider_reply(payload, "gpt-4.1-mini"))
    response = await service.generate(request)
    assert response.model == "gpt-4.1-mini" and not response.fallback_used
    assert len(response.days) <= 2
    assert service._provider.complete_json_result.await_args.kwargs["feature"] == "workouts"


@pytest.mark.asyncio
@pytest.mark.parametrize("service_type,model_request", [
    (PlanService, PlanGenerateRequest(objective="manutencao")),
    (WorkoutService, WorkoutGenerateRequest(objective="saude", days_per_week=2)),
])
async def test_invalid_model_schema_uses_deterministic_fallback(service_type, model_request):
    service = service_type(Settings())
    service._provider.complete_json_result = AsyncMock(return_value=provider_reply({"unexpected": "field"}))
    response = await service.generate(model_request)
    assert response.fallback_used and response.model.startswith("equilibrafit-")


@pytest.mark.asyncio
async def test_image_meal_always_requires_review_and_preserves_png_mime():
    service = MealRecognitionService(Settings())
    payload = {"confidence": 99, "items": [], "requires_user_review": False,
               "model": "untrusted", "fallback_used": True, "message": "measured"}
    service._provider.analyze_image_json_result = AsyncMock(return_value=provider_reply(payload))
    response = await service.recognize(MealRecognizeRequest(image_base64=PNG))
    assert response.model == "gemini-3.5-flash-lite" and not response.fallback_used
    assert response.requires_user_review and "nao medidos" in response.message
    assert service._provider.analyze_image_json_result.await_args.kwargs["mime_type"] == "image/png"


@pytest.mark.asyncio
async def test_text_meal_uses_text_not_image_operation():
    service = MealRecognitionService(Settings())
    payload = {"items": [], "model": "untrusted", "fallback_used": True, "message": ""}
    service._provider.complete_json_result = AsyncMock(return_value=provider_reply(payload))
    service._provider.analyze_image_json_result = AsyncMock()
    response = await service.estimate_text(MealTextEstimateRequest(description="pao com ovo"))
    assert response.model == "gemini-3.5-flash-lite" and not response.fallback_used
    assert service._provider.complete_json_result.await_args.kwargs["feature"] == "meal_text"
    service._provider.analyze_image_json_result.assert_not_awaited()


@pytest.mark.asyncio
async def test_yolo_classification_does_not_invent_macros(monkeypatch):
    service = MealRecognitionService(Settings())
    service._provider.analyze_image_json_result = AsyncMock(return_value=AiProviderResult())
    monkeypatch.setattr(service, "_detect_with_yolo", lambda _: ["banana"])
    response = await service.recognize(MealRecognizeRequest(image_base64=PNG))
    assert response.items == [] and response.fallback_used and response.requires_user_review
    assert "banana" in response.message and "nao estima" in response.message


@pytest.mark.asyncio
async def test_unknown_text_does_not_invent_nutrition():
    response = await MealRecognitionService(Settings()).estimate_text(
        MealTextEstimateRequest(description="alimento desconhecido"))
    assert response.items == [] and response.fallback_used and "manualmente" in response.message


@pytest.mark.asyncio
async def test_label_image_uses_actual_provider_and_requires_review():
    service = LabelRecognitionService(Settings())
    payload = {"calories": 150, "confidence": 99, "requires_user_review": False,
               "model": "untrusted", "fallback_used": True, "message": ""}
    service._provider.analyze_image_json_result = AsyncMock(return_value=provider_reply(payload))
    response = await service.recognize(LabelRecognizeRequest(image_base64=PNG, label_context="porcao de 30g"))
    assert response.calories == 150 and response.requires_user_review
    assert response.model == "gemini-3.5-flash-lite" and not response.fallback_used
    assert service._provider.analyze_image_json_result.await_args.kwargs["feature"] == "labels"
    assert "porcao de 30g" in service._provider.analyze_image_json_result.await_args.kwargs["user_prompt"]


@pytest.mark.parametrize("value", [float("inf"), float("nan"), -1])
def test_nutritional_schemas_reject_invalid_numbers(value):
    with pytest.raises(ValidationError):
        RecognizedFoodItem(name="x", portion=1, unit="g", calories=value,
                           protein_g=0, carbs_g=0, fat_g=0, confidence=80)
    with pytest.raises(ValidationError):
        LabelRecognizeResponse(calories=value, confidence=80, requires_user_review=True, message="")


def test_plan_rejects_inverted_calorie_bounds():
    with pytest.raises(ValidationError):
        PlanGenerateRequest(objective="saude", min_calories=2200, max_calories=1800)

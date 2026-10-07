from fastapi import APIRouter

from app.core.config import get_settings
from app.schemas.meals import (
    MealRecognizeRequest,
    MealRecognizeResponse,
    MealTextEstimateRequest,
    MealTextEstimateResponse,
)
from app.services.meal_recognition_service import MealRecognitionService

router = APIRouter(tags=["meals"])


@router.post("/api/v1/meals/recognize", response_model=MealRecognizeResponse)
async def recognize_meal(request: MealRecognizeRequest) -> MealRecognizeResponse:
    """Recognize a meal image."""
    return await MealRecognitionService(get_settings()).recognize(request)


@router.post("/api/v1/meals/estimate-text", response_model=MealTextEstimateResponse)
async def estimate_meal_text(request: MealTextEstimateRequest) -> MealTextEstimateResponse:
    """Estimate meal calories and macros from text."""
    return await MealRecognitionService(get_settings()).estimate_text(request)

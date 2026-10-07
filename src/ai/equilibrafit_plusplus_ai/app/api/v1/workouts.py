from fastapi import APIRouter

from app.core.config import get_settings
from app.schemas.workouts import WorkoutGenerateRequest, WorkoutGenerateResponse
from app.services.workout_service import WorkoutService

router = APIRouter(tags=["workouts"])


@router.post("/api/v1/workouts/generate", response_model=WorkoutGenerateResponse)
async def generate_workout(request: WorkoutGenerateRequest) -> WorkoutGenerateResponse:
    """Generate a safe workout proposal."""
    return await WorkoutService(get_settings()).generate(request)

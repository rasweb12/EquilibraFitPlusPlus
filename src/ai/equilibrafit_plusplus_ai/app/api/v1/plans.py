from fastapi import APIRouter

from app.core.config import get_settings
from app.schemas.plans import PlanGenerateRequest, PlanGenerateResponse
from app.services.plan_service import PlanService

router = APIRouter(tags=["plans"])


@router.post("/api/v1/plans/generate", response_model=PlanGenerateResponse)
async def generate_plan(request: PlanGenerateRequest) -> PlanGenerateResponse:
    """Generate a safe food plan proposal."""
    return await PlanService(get_settings()).generate(request)

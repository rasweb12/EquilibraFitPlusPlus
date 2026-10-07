from fastapi import APIRouter

from app.core.config import get_settings
from app.schemas.coach import CoachMessageRequest, CoachMessageResponse
from app.services.coach_service import CoachService

router = APIRouter(tags=["coach"])


@router.post("/api/v1/coach/message", response_model=CoachMessageResponse)
async def coach_message(request: CoachMessageRequest) -> CoachMessageResponse:
    """Reply to a Coach IA message."""
    return await CoachService(get_settings()).reply(request)


@router.post("/api/v1/coach/chat", response_model=CoachMessageResponse)
async def coach_chat(request: CoachMessageRequest) -> CoachMessageResponse:
    """Canonical chat endpoint consumed by the ASP.NET Core backend."""
    return await CoachService(get_settings()).reply(request)


@router.post("/v1/coach/chat", response_model=CoachMessageResponse)
async def backend_coach_chat(request: CoachMessageRequest) -> CoachMessageResponse:
    """Legacy compatibility endpoint consumed by earlier backend builds."""
    return await CoachService(get_settings()).reply(request)

from fastapi import APIRouter

from app.core.config import get_settings
from app.schemas.labels import LabelRecognizeRequest, LabelRecognizeResponse
from app.services.label_service import LabelRecognitionService

router = APIRouter(tags=["labels"])


@router.post("/api/v1/labels/recognize", response_model=LabelRecognizeResponse)
async def recognize_label(request: LabelRecognizeRequest) -> LabelRecognizeResponse:
    """Recognize a nutrition label."""
    return await LabelRecognitionService(get_settings()).recognize(request)

from pydantic import BaseModel, ConfigDict, Field


class LabelRecognizeRequest(BaseModel):
    """Nutrition label recognition request."""

    image_base64: str | None = None
    extracted_text: str | None = None
    label_context: str | None = Field(default=None, max_length=500)


class LabelRecognizeResponse(BaseModel):
    """Nutrition label recognition response."""
    model_config = ConfigDict(allow_inf_nan=False)

    serving_size: str | None = None
    calories: float | None = Field(default=None, ge=0)
    protein_g: float | None = Field(default=None, ge=0)
    carbs_g: float | None = Field(default=None, ge=0)
    fat_g: float | None = Field(default=None, ge=0)
    confidence: float = Field(ge=0, le=100)
    requires_user_review: bool
    model: str = "equilibrafit-labels-rules-v1"
    fallback_used: bool = True
    message: str

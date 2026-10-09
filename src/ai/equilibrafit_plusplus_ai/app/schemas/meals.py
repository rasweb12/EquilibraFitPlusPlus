from pydantic import BaseModel, ConfigDict, Field


class MealRecognizeRequest(BaseModel):
    """Meal recognition request."""

    image_base64: str | None = None
    image_url: str | None = None
    meal_context: str | None = None


class MealTextEstimateRequest(BaseModel):
    """Text meal estimation request."""

    description: str = Field(min_length=3, max_length=600)
    meal_type: str | None = None


class RecognizedFoodItem(BaseModel):
    """Recognized food item."""
    model_config = ConfigDict(allow_inf_nan=False)

    name: str
    portion: float = Field(ge=0)
    unit: str
    calories: float = Field(ge=0)
    protein_g: float = Field(ge=0)
    carbs_g: float = Field(ge=0)
    fat_g: float = Field(ge=0)
    confidence: float = Field(ge=0, le=100)


class MealRecognizeResponse(BaseModel):
    """Meal recognition response."""

    confidence: float = Field(ge=0, le=100)
    items: list[RecognizedFoodItem]
    requires_user_review: bool
    model: str
    fallback_used: bool
    message: str


class MealTextEstimateResponse(BaseModel):
    """Text meal estimation response."""

    items: list[RecognizedFoodItem]
    model: str
    fallback_used: bool
    message: str

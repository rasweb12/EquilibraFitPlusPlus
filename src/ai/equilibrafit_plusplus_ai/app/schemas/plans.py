from pydantic import BaseModel, Field

from app.schemas.common import MacroTargets, SafetyNotice


class PlanGenerateRequest(BaseModel):
    """Diet plan generation request."""

    objective: str
    routine: str | None = None
    preferences: list[str] = Field(default_factory=list)
    restrictions: list[str] = Field(default_factory=list)
    min_calories: int = Field(default=1400, ge=1000, le=6000)
    max_calories: int = Field(default=2400, ge=1000, le=6000)
    protein_target_g: float | None = Field(default=None, ge=0)
    operational_guidance: str | None = Field(default=None, max_length=12000)


class MealSuggestion(BaseModel):
    """Meal suggestion."""

    name: str
    description: str
    calories: int


class PlanGenerateResponse(BaseModel):
    """Structured plan response."""

    targets: MacroTargets
    meals: list[MealSuggestion]
    explanation: str
    alternatives: list[str]
    safety_notices: list[SafetyNotice]
    model: str
    fallback_used: bool

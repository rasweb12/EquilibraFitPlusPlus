from pydantic import BaseModel, Field, model_validator

from app.schemas.common import MacroTargets, SafetyNotice


class PlanGenerateRequest(BaseModel):
    """Diet plan generation request."""

    objective: str
    routine: str | None = None
    preferences: list[str] = Field(default_factory=list)
    restrictions: list[str] = Field(default_factory=list)
    min_calories: int = Field(default=1400, ge=1000, le=6000)
    max_calories: int = Field(default=2400, ge=1000, le=6000)
    protein_target_g: float | None = Field(default=None, ge=0, allow_inf_nan=False)
    operational_guidance: str | None = Field(default=None, max_length=12000)

    @model_validator(mode="after")
    def ordered_calorie_bounds(self):
        if self.min_calories > self.max_calories:
            raise ValueError("min_calories must not exceed max_calories")
        return self


class MealSuggestion(BaseModel):
    """Meal suggestion."""

    name: str
    description: str
    calories: int = Field(ge=0)


class PlanGenerateResponse(BaseModel):
    """Structured plan response."""

    targets: MacroTargets
    meals: list[MealSuggestion]
    explanation: str
    alternatives: list[str]
    safety_notices: list[SafetyNotice]
    model: str
    fallback_used: bool

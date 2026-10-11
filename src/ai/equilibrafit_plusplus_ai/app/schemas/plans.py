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


class RecipeIngredient(BaseModel):
    """Ingredient and quantity for an educational recipe."""

    name: str = Field(min_length=1, max_length=120)
    quantity: str = Field(min_length=1, max_length=80)


class MealAlternative(BaseModel):
    """Alternative that can replace one plan meal after user confirmation."""

    name: str = Field(min_length=1, max_length=180)
    description: str = Field(min_length=1, max_length=800)
    calories: int | None = Field(default=None, ge=0)
    ingredients: list[RecipeIngredient] = Field(default_factory=list, max_length=12)


class QuickRecipe(BaseModel):
    """Quick educational recipe with concrete preparation details."""

    name: str = Field(min_length=1, max_length=180)
    ingredients: list[RecipeIngredient] = Field(min_length=1, max_length=12)
    preparation: list[str] = Field(min_length=1, max_length=8)
    preparation_minutes: int = Field(ge=1, le=120)


class MealSuggestion(BaseModel):
    """Meal suggestion."""

    name: str
    description: str
    calories: int = Field(ge=0)
    alternatives: list[MealAlternative] = Field(default_factory=list, max_length=3)
    quick_recipe: QuickRecipe | None = None


class PlanGenerateResponse(BaseModel):
    """Structured plan response."""

    targets: MacroTargets
    meals: list[MealSuggestion]
    explanation: str
    alternatives: list[str]
    safety_notices: list[SafetyNotice]
    model: str
    fallback_used: bool

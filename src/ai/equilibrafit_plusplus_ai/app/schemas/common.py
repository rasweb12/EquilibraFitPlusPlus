from pydantic import BaseModel, Field


class SafetyNotice(BaseModel):
    """Safety and uncertainty notice returned to callers."""

    message: str
    requires_professional_review: bool = False


class MacroTargets(BaseModel):
    """Macronutrient targets."""

    calories: int = Field(ge=0)
    protein_g: float = Field(ge=0)
    carbs_g: float = Field(ge=0)
    fat_g: float = Field(ge=0)

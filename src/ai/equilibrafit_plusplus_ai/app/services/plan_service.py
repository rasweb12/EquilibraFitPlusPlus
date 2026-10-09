from app.core.config import Settings
from app.providers.ai_provider_router import AiProviderRouter
from app.schemas.common import MacroTargets, SafetyNotice
from app.schemas.plans import MealSuggestion, PlanGenerateRequest, PlanGenerateResponse


class PlanService:
    """Generates bounded food plan proposals."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._openai = AiProviderRouter(settings)

    async def generate(self, request: PlanGenerateRequest) -> PlanGenerateResponse:
        """Generate an AI plan proposal with deterministic hybrid fallback."""
        generated = await self._generate_with_ai(request)
        if generated is not None:
            return generated

        return self._generate_hybrid(request)

    async def _generate_with_openai(self, request: PlanGenerateRequest) -> PlanGenerateResponse | None:
        system_prompt = (
            "Você é o motor interno do EquilibraFit++. Gere orientação alimentar educacional, flexível e sem extremismos. "
            "Nunca use culpa, punição ou linguagem negativa. Não substitua nutricionistas ou médicos. "
            "Responda somente JSON válido no schema solicitado."
        )
        user_prompt = (
            "Gere um plano alimentar dentro destes limites e sem prometer tratamento clínico.\n"
            f"Objetivo: {request.objective}\n"
            f"Rotina: {request.routine or 'não informada'}\n"
            f"Preferências: {request.preferences}\n"
            f"Restrições: {request.restrictions}\n"
            f"Calorias mínimas: {request.min_calories}\n"
            f"Calorias máximas: {request.max_calories}\n"
            f"Proteína alvo g: {request.protein_target_g}\n"
            f"Instruções operacionais aprovadas: {request.operational_guidance or 'usar padrão seguro do produto'}\n"
            "JSON: {"
            "\"targets\":{\"calories\":int,\"protein_g\":float,\"carbs_g\":float,\"fat_g\":float},"
            "\"meals\":[{\"name\":string,\"description\":string,\"calories\":int}],"
            "\"explanation\":string,"
            "\"alternatives\":[string],"
            "\"safety_notices\":[{\"message\":string,\"requires_professional_review\":bool}],"
            "\"model\":string,"
            "\"fallback_used\":false"
            "}"
        )
        payload = await self._provider.complete_json(
            feature="plans",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )
        if payload is None:
            return None

        try:
            response = PlanGenerateResponse.model_validate(payload)
        except Exception:  # noqa: BLE001 - invalid model output must fallback safely
            return None

        bounded_calories = min(max(response.targets.calories, request.min_calories), request.max_calories)
        response.targets.calories = bounded_calories
        response.model = self._provider.last_model or self._settings.openai_model
        response.fallback_used = self._provider.fallback_used
        response.safety_notices.append(
            SafetyNotice(message="Esta proposta não substitui nutricionista ou médico.", requires_professional_review=False)
        )
        return response

    @staticmethod
    def _generate_hybrid(request: PlanGenerateRequest) -> PlanGenerateResponse:
        """Generate a deterministic safe plan proposal."""
        calories = round((request.min_calories + request.max_calories) / 2)
        protein = request.protein_target_g or round(calories * 0.22 / 4, 1)
        fat = round(calories * 0.28 / 9, 1)
        carbs = round((calories - protein * 4 - fat * 9) / 4, 1)
        restrictions = ", ".join(request.restrictions) if request.restrictions else "sem restrições informadas"

        meals = [
            MealSuggestion(name="Café da manhã flexível", description="Proteína leve, fruta e carboidrato simples de preparar.", calories=round(calories * 0.25)),
            MealSuggestion(name="Almoço equilibrado", description=f"Base com legumes, proteína e carboidrato ajustado; considerar {restrictions}.", calories=round(calories * 0.35)),
            MealSuggestion(name="Jantar tranquilo", description="Refeição com boa saciedade e preparo compatível com a rotina.", calories=round(calories * 0.30)),
            MealSuggestion(name="Lanche opcional", description="Opção simples para fome entre refeições, sem obrigatoriedade.", calories=round(calories * 0.10)),
        ]

        return PlanGenerateResponse(
            targets=MacroTargets(calories=calories, protein_g=protein, carbs_g=max(carbs, 0), fat_g=fat),
            meals=meals,
            explanation="Plano educacional dentro dos limites enviados pelo backend, ajustável conforme rotina e preferências.",
            alternatives=["Trocar fontes de carboidrato por equivalentes culturais.", "Ajustar horários sem tratar refeições como obrigação rígida."],
            safety_notices=[
                SafetyNotice(message="Esta proposta não substitui nutricionista ou médico.", requires_professional_review=False),
                SafetyNotice(message="Condições clínicas exigem acompanhamento profissional.", requires_professional_review=True),
            ],
            model="equilibrafit-plan-rules-v1",
            fallback_used=True,
        )

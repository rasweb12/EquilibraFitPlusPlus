import re
import unicodedata

from app.core.config import Settings
from app.providers.openai_provider import OpenAiTextProvider
from app.schemas.labels import LabelRecognizeRequest, LabelRecognizeResponse


class LabelRecognitionService:
    """Extracts nutrition facts from label text or sends image to review path."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._openai = OpenAiTextProvider(settings)

    async def recognize(self, request: LabelRecognizeRequest) -> LabelRecognizeResponse:
        """Recognize a nutrition label."""
        if request.extracted_text:
            return self._from_text(request.extracted_text)

        if request.image_base64 and self._openai.is_configured:
            openai_response = await self._recognize_with_openai(request.image_base64)
            if openai_response is not None:
                return openai_response

        return LabelRecognizeResponse(
            confidence=25.0,
            requires_user_review=True,
            model="equilibrafit-labels-rules-v1",
            fallback_used=True,
            message="Rótulo recebido. Ainda precisamos confirmar os dados manualmente antes de salvar.",
        )

    @staticmethod
    def _from_text(text: str) -> LabelRecognizeResponse:
        normalized = _normalize_text(text)

        return LabelRecognizeResponse(
            serving_size=_match_text(normalized, r"(porcao|serving)\s*[:\-]?\s*([0-9]+ ?[a-z]+)", group=2),
            calories=_match_number(normalized, r"(calorias|kcal|valor energetico)\D*([0-9]+(?:\.[0-9]+)?)"),
            protein_g=_match_number(normalized, r"(proteinas?|protein)\D*([0-9]+(?:\.[0-9]+)?)"),
            carbs_g=_match_number(normalized, r"(carboidratos?|carbs?)\D*([0-9]+(?:\.[0-9]+)?)"),
            fat_g=_match_number(normalized, r"(gorduras totais|fat)\D*([0-9]+(?:\.[0-9]+)?)"),
            confidence=70.0,
            requires_user_review=True,
            model="equilibrafit-labels-text-v1",
            fallback_used=False,
            message="Dados extraídos do texto. Confirme as informações antes de salvar.",
        )

    async def _recognize_with_openai(self, image_base64: str) -> LabelRecognizeResponse | None:
        system_prompt = (
            "Você é o motor de OCR nutricional do EquilibraFit++. Extraia apenas dados visíveis do rótulo. "
            "Quando algo não estiver claro, use null e exija revisão do usuário. Responda somente JSON válido."
        )
        user_prompt = (
            "Extraia dados nutricionais por porção da imagem.\n"
            "JSON: {"
            "\"serving_size\":string|null,"
            "\"calories\":float|null,"
            "\"protein_g\":float|null,"
            "\"carbs_g\":float|null,"
            "\"fat_g\":float|null,"
            "\"confidence\":float,"
            "\"requires_user_review\":bool,"
            "\"model\":string,"
            "\"fallback_used\":false,"
            "\"message\":string"
            "}"
        )
        payload = await self._openai.analyze_image_json(
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            image_base64=image_base64,
        )
        if payload is None:
            return None

        try:
            response = LabelRecognizeResponse.model_validate(payload)
        except Exception:  # noqa: BLE001 - invalid model output must fallback safely
            return None

        response.model = self._settings.openai_model
        response.fallback_used = False
        response.requires_user_review = True
        response.message = response.message or "Dados extraídos do rótulo. Confirme antes de salvar."
        return response


def _match_number(text: str, pattern: str) -> float | None:
    match = re.search(pattern, text)
    return float(match.group(2)) if match else None


def _match_text(text: str, pattern: str, group: int) -> str | None:
    match = re.search(pattern, text)
    return match.group(group) if match else None


def _normalize_text(value: str) -> str:
    decomposed = unicodedata.normalize("NFD", value.lower().replace(",", "."))
    return "".join(char for char in decomposed if unicodedata.category(char) != "Mn")

import logging
import re
import unicodedata

from pydantic import ValidationError

from app.core.config import Settings
from app.providers.ai_provider_router import AiProviderRouter
from app.providers.image_input import decode_image
from app.schemas.labels import LabelRecognizeRequest, LabelRecognizeResponse

logger = logging.getLogger(__name__)


class LabelRecognitionService:
    """Extract nutrition facts from text or image, always requiring user review."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._provider = AiProviderRouter(settings)

    async def recognize(self, request: LabelRecognizeRequest) -> LabelRecognizeResponse:
        """Recognize a nutrition label without inventing unreadable values."""
        if request.extracted_text and request.extracted_text.strip():
            return self._from_text(request.extracted_text)

        if request.image_base64:
            result = await self._recognize_with_ai(request.image_base64, request.label_context)
            if result is not None:
                return result

        return LabelRecognizeResponse(
            confidence=0.0,
            requires_user_review=True,
            model="equilibrafit-labels-rules-v1",
            fallback_used=True,
            message="Não foi possível extrair o rótulo com segurança. Confira e informe os valores manualmente.",
        )

    @staticmethod
    def _from_text(text: str) -> LabelRecognizeResponse:
        normalized = _normalize_text(text)
        serving_size = _match_text(normalized, r"(?:porcao|serving)\s*[:\-]?\s*([0-9]+(?:\.[0-9]+)?\s*[a-z]+)")
        calories = _match_number(normalized, r"(?:calorias|kcal|valor energetico)\D*([0-9]+(?:\.[0-9]+)?)")
        protein_g = _match_number(normalized, r"(?:proteinas?|protein)\D*([0-9]+(?:\.[0-9]+)?)")
        carbs_g = _match_number(normalized, r"(?:carboidratos?|carbs?)\D*([0-9]+(?:\.[0-9]+)?)")
        fat_g = _match_number(normalized, r"(?:gorduras totais|fat)\D*([0-9]+(?:\.[0-9]+)?)")
        extracted = [calories, protein_g, carbs_g, fat_g]
        count = sum(value is not None for value in extracted)
        return LabelRecognizeResponse(
            serving_size=serving_size,
            calories=calories,
            protein_g=protein_g,
            carbs_g=carbs_g,
            fat_g=fat_g,
            confidence=round(70.0 * count / 4, 1),
            requires_user_review=True,
            model="equilibrafit-labels-text-v1",
            fallback_used=False,
            message="Dados extraídos do texto. Confirme a porção e os valores antes de salvar." if count else "Nenhum valor nutricional reconhecido; preencha manualmente.",
        )

    async def _recognize_with_ai(self, image_base64: str, label_context: str | None = None) -> LabelRecognizeResponse | None:
        # The providers accept raw base64 bytes; data URLs are intentionally rejected.
        decoded = decode_image(image_base64)
        if decoded is None:
            return None
        mime_type = decoded[1]

        system_prompt = (
            "Você extrai dados nutricionais de rótulos para o EquilibraFit++. "
            "Extraia exclusivamente informações legíveis da imagem e relativas à mesma porção. "
            "Não invente valores. Para campos ilegíveis, retorne null. "
            "Responda somente com um objeto JSON válido."
        )
        user_prompt = (
            "Leia a tabela nutricional e devolva JSON com: "
            "serving_size (string ou null), calories (number ou null), "
            "protein_g (number ou null), carbs_g (number ou null), "
            "fat_g (number ou null), confidence (number de 0 a 100), "
            "requires_user_review (true), model (string), "
            "fallback_used (false) e message (string). "
            "Use a mesma porção para todos os valores. "
            "Não deduza dados ausentes e não use valores por 100 g como se fossem por porção."
            f"\nContexto do usuario (nao e texto extraido do rotulo): {label_context or 'nao informado'}"
        )
        result = await self._provider.analyze_image_json_result(
            feature="labels",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            image_base64=image_base64,
            mime_type=mime_type,
        )
        if result.payload is None:
            return None
        try:
            response = LabelRecognizeResponse.model_validate(result.payload)
        except (ValidationError, ValueError, TypeError):
            logger.warning("Invalid nutrition label AI response; requesting manual review")
            return None

        # Never trust the model to supply provider metadata or review requirements.
        response.model = result.model
        response.fallback_used = result.fallback_used
        response.requires_user_review = True
        response.confidence = min(max(response.confidence, 0.0), 100.0)
        response.message = "Dados extraídos do rótulo. Confira porção e valores antes de salvar."
        return response


def _match_number(text: str, pattern: str) -> float | None:
    match = re.search(pattern, text)
    return float(match.group(1)) if match else None


def _match_text(text: str, pattern: str) -> str | None:
    match = re.search(pattern, text)
    return match.group(1).strip() if match else None


def _normalize_text(value: str) -> str:
    decomposed = unicodedata.normalize("NFD", value.lower().replace(",", "."))
    return "".join(char for char in decomposed if unicodedata.category(char) != "Mn")

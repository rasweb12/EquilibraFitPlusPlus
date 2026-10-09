import base64
import logging
import re
import unicodedata
from app.core.config import Settings
from app.providers.ai_provider_router import AiProviderRouter
from app.schemas.meals import (
    MealRecognizeRequest,
    MealRecognizeResponse,
    MealTextEstimateRequest,
    MealTextEstimateResponse,
    RecognizedFoodItem,
)
from app.services.safety_service import SafetyService
logger = logging.getLogger(__name__)
class MealRecognitionService:
    """Recognizes meals using optional YOLO/OpenCV and safe fallback."""
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._safety = SafetyService()
        self._provider = AiProviderRouter(settings)
        self._model = self._load_yolo_model(settings.yolo_model_path)
    async def recognize(self, request: MealRecognizeRequest) -> MealRecognizeResponse:
        """Recognize food items in a meal image."""
        if not request.image_base64 and not request.image_url:
            return self._manual_review_response("Imagem não informada. Você pode registrar a refeição manualmente.")
        if request.image_base64:
            binary = self._decode_image(request.image_base64)
            if binary is None:
                return self._manual_review_response(self._safety.safe_fallback("meal"))
            ai_response = await self._recognize_with_ai(request)
            if ai_response is not None:
                return ai_response
        detected = self._detect_with_yolo(request.image_base64) if request.image_base64 else []
        if detected:
            confidence = min(95.0, max(item.confidence for item in detected))
            return MealRecognizeResponse(
                confidence=confidence,
                items=detected,
                requires_user_review=True,
                model=self._settings.yolo_model_path or "yolo",
                fallback_used=False,
                message="Itens estimados. Confirme porções antes de salvar.",
            )
        return self._manual_review_response(
            "Não foi possível interpretar esta foto com segurança. "
            "Nenhum valor nutricional foi estimado; registre os itens manualmente."
        )
    async def estimate_text(self, request: MealTextEstimateRequest) -> MealTextEstimateResponse:
        """Estimate food items from a textual meal description."""
        description = request.description.strip()
        ai_response = await self._estimate_text_with_ai(request)
        if ai_response is not None:
            return ai_response
        items = self._estimate_text_with_rules(description)
        return MealTextEstimateResponse(
            items=items,
            model="equilibrafit-meal-text-rules-v1",
            fallback_used=True,
            message="Estimativa criada. Revise porções e ajuste antes de salvar.",
        )
    def _manual_review_response(self, message: str) -> MealRecognizeResponse:
        return MealRecognizeResponse(
            confidence=0,
            items=[],
            requires_user_review=True,
            model="equilibrafit-vision-rules-v1",
            fallback_used=True,
            message=message,
        )
    @staticmethod
    def _decode_image(image_base64: str) -> bytes | None:
        try:
            binary = base64.b64decode(image_base64, validate=True)
            return binary if binary else None
        except Exception as exception:  # noqa: BLE001 - corrupted images must return review path
            logger.warning("Meal image decoding failed: %s", exception.__class__.__name__)
            return None
    async def _recognize_with_ai(self, request: MealRecognizeRequest) -> MealRecognizeResponse | None:
        if not request.image_base64:
            return None
        system_prompt = (
            "Você é o motor de visão do EquilibraFit++. Identifique alimentos visíveis com incerteza explícita. "
            "Nunca afirme diagnóstico, nunca julgue a refeição e sempre exija revisão do usuário quando houver dúvida. "
            "Responda somente JSON válido no schema solicitado."
        )
        user_prompt = (
            "Analise a imagem de refeição. Estime porções de forma conservadora e educativa.\n"
            f"Contexto: {request.meal_context or 'não informado'}\n"
            "JSON: {"
            "\"confidence\":float,"
            "\"items\":[{\"name\":string,\"portion\":float,\"unit\":string,\"calories\":float,"
            "\"protein_g\":float,\"carbs_g\":float,\"fat_g\":float,\"confidence\":float}],"
            "\"requires_user_review\":bool,"
            "\"model\":string,"
            "\"fallback_used\":false,"
            "\"message\":string"
            "}"
        )
        payload = await self._provider.analyze_image_json(
            feature="meals",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            image_base64=request.image_base64,
        )
        if payload is None:
            return None
        try:
            response = MealRecognizeResponse.model_validate(payload)
        except Exception:  # noqa: BLE001 - invalid model output must fallback safely
            return None
        response.model = self._provider.last_model or self._settings.model_for_provider(self._settings.provider)
        response.fallback_used = self._provider.fallback_used
        response.requires_user_review = response.requires_user_review or response.confidence < 85
        response.message = response.message or "Itens estimados. Confirme porções antes de salvar."
        return response
    async def _estimate_text_with_ai(self, request: MealTextEstimateRequest) -> MealTextEstimateResponse | None:
        system_prompt = (
            "Você é o motor nutricional do EquilibraFit++. Estime calorias e macros de forma conservadora, "
            "sem julgamento e sem substituir nutricionista. Responda somente JSON válido no schema solicitado."
        )
        user_prompt = (
            "Estime a refeição descrita abaixo usando porções comuns no Brasil. "
            "Se houver dúvida, use item genérico e confidence menor.\n"
            f"Tipo: {request.meal_type or 'não informado'}\n"
            f"Descrição: {request.description}\n"
            "JSON: {"
            "\"items\":[{\"name\":string,\"portion\":float,\"unit\":string,\"calories\":float,"
            "\"protein_g\":float,\"carbs_g\":float,\"fat_g\":float,\"confidence\":float}],"
            "\"model\":string,"
            "\"fallback_used\":false,"
            "\"message\":string"
            "}"
        )
        payload = await self._provider.complete_json(
            feature="meal_text",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )
        if payload is None:
            return None
        try:
            response = MealTextEstimateResponse.model_validate(payload)
        except Exception:  # noqa: BLE001 - invalid model output must fallback safely
            return None
        response.model = self._provider.last_model or self._settings.model_for_provider(self._settings.provider)
        response.fallback_used = self._provider.fallback_used
        response.message = response.message or "Estimativa criada. Revise porções e ajuste antes de salvar."
        return response
    def _estimate_text_with_rules(self, description: str) -> list[RecognizedFoodItem]:
        normalized = self._normalize(description)
        items: list[RecognizedFoodItem] = []
        bread_term = self._normalize("pão")
        if bread_term in normalized:
            breads = max(1, self._extract_quantity_before(normalized, bread_term))
            items.append(
                RecognizedFoodItem(
                    name="pão francês",
                    portion=float(breads),
                    unit="unidade",
                    calories=140 * breads,
                    protein_g=4.5 * breads,
                    carbs_g=28 * breads,
                    fat_g=1.5 * breads,
                    confidence=72.0,
                )
            )
        if "ovo" in normalized:
            eggs = max(1, self._extract_quantity_before(normalized, "ovo"))
            items.append(
                RecognizedFoodItem(
                    name="ovo",
                    portion=float(eggs),
                    unit="unidade",
                    calories=78 * eggs,
                    protein_g=6.3 * eggs,
                    carbs_g=0.6 * eggs,
                    fat_g=5.3 * eggs,
                    confidence=78.0,
                )
            )
        if "arroz" in normalized:
            items.append(self._portion("arroz cozido", 100, "g", 128, 2.5, 28, 0.2, 65.0))
        if self._normalize("feijão") in normalized:
            items.append(self._portion("feijão cozido", 100, "g", 76, 4.8, 13.6, 0.5, 65.0))
        if any(term in normalized for term in ["frango", "peito de frango"]):
            items.append(self._portion("frango grelhado", 100, "g", 165, 31, 0, 3.6, 68.0))
        if any(term in normalized for term in ["salada", "alface", "tomate"]):
            items.append(self._portion("salada simples", 1, "porção", 35, 1.5, 6, 0.5, 60.0))
        if not items:
            items.append(
                RecognizedFoodItem(
                    name="refeição descrita",
                    portion=1,
                    unit="porção",
                    calories=350,
                    protein_g=18,
                    carbs_g=42,
                    fat_g=12,
                    confidence=45.0,
                )
            )
        return items
    @staticmethod
    def _portion(
        name: str,
        portion: float,
        unit: str,
        calories: float,
        protein_g: float,
        carbs_g: float,
        fat_g: float,
        confidence: float,
    ) -> RecognizedFoodItem:
        return RecognizedFoodItem(
            name=name,
            portion=portion,
            unit=unit,
            calories=calories,
            protein_g=protein_g,
            carbs_g=carbs_g,
            fat_g=fat_g,
            confidence=confidence,
        )
    @staticmethod
    def _normalize(value: str) -> str:
        without_accents = "".join(
            char for char in unicodedata.normalize("NFD", value.lower()) if unicodedata.category(char) != "Mn"
        )
        return re.sub(r"\s+", " ", without_accents)
    @staticmethod
    def _extract_quantity_before(value: str, keyword: str) -> int:
        match = re.search(rf"(\d+)\s+(?:unidades?\s+de\s+)?{re.escape(keyword)}", value)
        return int(match.group(1)) if match else 1
    @staticmethod
    def _load_yolo_model(model_path: str | None):
        if not model_path:
            return None
        try:
            from ultralytics import YOLO
            return YOLO(model_path)
        except Exception as exception:  # noqa: BLE001 - optional provider must degrade gracefully
            logger.warning("YOLO model could not be loaded: %s", exception.__class__.__name__)
            return None
    def _detect_with_yolo(self, image_base64: str | None) -> list[RecognizedFoodItem]:
        if self._model is None or not image_base64:
            return []
        try:
            import cv2
            import numpy as np
            binary = base64.b64decode(image_base64, validate=True)
            array = np.frombuffer(binary, dtype=np.uint8)
            image = cv2.imdecode(array, cv2.IMREAD_COLOR)
            results = self._model.predict(image, verbose=False)
            items: list[RecognizedFoodItem] = []
            for result in results:
                names = getattr(result, "names", {})
                boxes = getattr(result, "boxes", [])
                for box in boxes:
                    confidence = float(box.conf[0]) * 100
                    class_id = int(box.cls[0])
                    name = str(names.get(class_id, "alimento"))
                    items.append(
                        RecognizedFoodItem(
                            name=name,
                            portion=100,
                            unit="g",
                            calories=180,
                            protein_g=8,
                            carbs_g=22,
                            fat_g=6,
                            confidence=round(confidence, 2),
                        )
                    )
            return items
        except Exception as exception:  # noqa: BLE001
            logger.warning("YOLO meal detection failed: %s", exception.__class__.__name__)
            return []

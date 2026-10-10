
import base64
import binascii
import json
import logging
import re

from app.core.config import Settings
from app.providers.provider_errors import AiProviderError

logger = logging.getLogger(__name__)


class GeminiProvider:
    """Google Gemini provider for text, JSON and image analysis."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._client = None
        self._types = None

        api_key = getattr(settings, "gemini_api_key", None)

        if not api_key:
            return

        try:
            from google import genai
            from google.genai import types

            self._client = genai.Client(
                api_key=api_key,
                http_options=types.HttpOptions(
                    timeout=settings.request_timeout_seconds * 1000,
                ),
            )
            self._types = types

        except ImportError:
            logger.warning(
                "google-genai is not installed."
            )

    @property
    def is_configured(self) -> bool:
        return self._client is not None

    @property
    def model(self) -> str:
        return getattr(
            self._settings,
            "gemini_model",
            "gemini-3.5-flash-lite",
        )

    async def complete(
        self,
        *,
        system_prompt: str,
        user_prompt: str,
        model: str | None = None,
    ) -> str | None:
        """Generate text with Gemini."""

        if not self.is_configured:
            return None

        try:
            response = await self._client.aio.models.generate_content(
                model=model or self.model,
                contents=user_prompt,
                config=self._types.GenerateContentConfig(
                    system_instruction=system_prompt,
                    temperature=0.4,
                    max_output_tokens=(
                        self._settings.ai_max_output_tokens
                    ),
                ),
            )

            content = response.text

            return content.strip() if content else None

        except Exception as exc:  # noqa: BLE001 - optional SDK failures must degrade safely
            raise AiProviderError.from_exception(exc) from None

    async def complete_json(
        self,
        *,
        system_prompt: str,
        user_prompt: str,
        model: str | None = None,
    ) -> dict | None:
        """Generate a JSON object with Gemini."""

        if not self.is_configured:
            return None

        try:
            response = await self._client.aio.models.generate_content(
                model=model or self.model,
                contents=user_prompt,
                config=self._types.GenerateContentConfig(
                    system_instruction=system_prompt,
                    response_mime_type="application/json",
                    temperature=0.2,
                    max_output_tokens=(
                        self._settings.ai_max_output_tokens
                    ),
                ),
            )

            return self._parse_json(response.text)

        except Exception as exc:  # noqa: BLE001 - invalid provider responses use safe fallback
            raise AiProviderError.from_exception(exc) from None

    async def analyze_image_json(
        self,
        *,
        system_prompt: str,
        user_prompt: str,
        image_base64: str,
        mime_type: str = "image/jpeg",
        model: str | None = None,
    ) -> dict | None:
        """Analyze an image using Gemini multimodal input."""

        if not self.is_configured:
            return None

        allowed_types = {
            "image/jpeg",
            "image/png",
            "image/webp",
        }

        if mime_type not in allowed_types:
            return None

        try:
            image_data = base64.b64decode(
                image_base64,
                validate=True,
            )
        except (binascii.Error, ValueError):
            return None

        if not image_data or len(image_data) > 10 * 1024 * 1024:
            return None

        try:
            image_part = self._types.Part.from_bytes(
                data=image_data,
                mime_type=mime_type,
            )

            response = await self._client.aio.models.generate_content(
                model=model or self.model,
                contents=[
                    user_prompt,
                    image_part,
                ],
                config=self._types.GenerateContentConfig(
                    system_instruction=system_prompt,
                    response_mime_type="application/json",
                    temperature=0.2,
                    max_output_tokens=(
                        self._settings.ai_max_output_tokens
                    ),
                ),
            )

            return self._parse_json(response.text)

        except Exception as exc:  # noqa: BLE001 - vision failures must require manual review
            raise AiProviderError.from_exception(exc) from None

    @staticmethod
    def _parse_json(text: str | None) -> dict | None:
        """Parse a JSON object without accepting arbitrary output."""

        if not text:
            return None

        candidate = text.strip()

        candidate = re.sub(
            r"^```(?:json)?\s*",
            "",
            candidate,
            flags=re.IGNORECASE,
        )

        candidate = re.sub(
            r"\s*```$",
            "",
            candidate,
        )

        try:
            parsed = json.loads(candidate)

            return parsed if isinstance(parsed, dict) else None

        except json.JSONDecodeError:
            return None

import json
import logging
import re

from app.core.config import Settings

logger = logging.getLogger(__name__)


class OpenAiTextProvider:
    """OpenAI text provider with safe optional activation."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._client = None

        if not settings.openai_api_key:
            return

        try:
            from openai import AsyncOpenAI

            self._client = AsyncOpenAI(api_key=settings.openai_api_key, timeout=settings.request_timeout_seconds)
        except ImportError:
            logger.warning("OpenAI package is not installed; fallback responses will be used.")

    @property
    def is_configured(self) -> bool:
        """Return whether OpenAI is configured and importable."""
        return self._client is not None

    async def complete(self, *, system_prompt: str, user_prompt: str, model: str | None = None) -> str | None:
        """Generate text using OpenAI Responses API when available."""
        if self._client is None:
            return None

        try:
            response = await self._client.responses.create(
                model=model or self._settings.openai_model,
                input=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
                temperature=0.4,
            )
            output_text = getattr(response, "output_text", None)
            return output_text.strip() if output_text else None
        except Exception as exception:  # noqa: BLE001 - provider failures must degrade gracefully
            logger.warning("OpenAI provider failed: %s", exception.__class__.__name__)
            return None

    async def complete_json(self, *, system_prompt: str, user_prompt: str, model: str | None = None) -> dict | None:
        """Generate and parse a JSON object from OpenAI."""
        text = await self.complete(system_prompt=system_prompt, user_prompt=user_prompt, model=model)
        return _extract_json_object(text)

    async def analyze_image_json(
        self,
        *,
        system_prompt: str,
        user_prompt: str,
        image_base64: str,
        mime_type: str = "image/jpeg",
        model: str | None = None,
    ) -> dict | None:
        """Analyze a Base64 image and parse a JSON object from OpenAI."""
        if self._client is None:
            return None

        try:
            response = await self._client.responses.create(
                model=model or self._settings.openai_model,
                input=[
                    {
                        "role": "system",
                        "content": [{"type": "input_text", "text": system_prompt}],
                    },
                    {
                        "role": "user",
                        "content": [
                            {"type": "input_text", "text": user_prompt},
                            {
                                "type": "input_image",
                                "image_url": f"data:{mime_type};base64,{image_base64}",
                            },
                        ],
                    },
                ],
                temperature=0.2,
            )
            output_text = getattr(response, "output_text", None)
            return _extract_json_object(output_text)
        except Exception as exception:  # noqa: BLE001 - provider failures must degrade gracefully
            logger.warning("OpenAI vision provider failed: %s", exception.__class__.__name__)
            return None


def _extract_json_object(text: str | None) -> dict | None:
    if not text:
        return None

    candidate = text.strip()
    if candidate.startswith("```"):
        candidate = re.sub(r"^```(?:json)?", "", candidate, flags=re.IGNORECASE).strip()
        candidate = re.sub(r"```$", "", candidate).strip()

    try:
        parsed = json.loads(candidate)
        return parsed if isinstance(parsed, dict) else None
    except json.JSONDecodeError:
        match = re.search(r"\{.*\}", candidate, flags=re.DOTALL)
        if not match:
            return None

        try:
            parsed = json.loads(match.group(0))
            return parsed if isinstance(parsed, dict) else None
        except json.JSONDecodeError:
            return None

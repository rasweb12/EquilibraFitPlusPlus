import asyncio
import logging
from dataclasses import dataclass
from typing import Any

from app.core.config import AiProviderName, Settings
from app.providers.gemini_provider import GeminiProvider
from app.providers.openai_provider import OpenAiTextProvider
from app.providers.provider_errors import AiProviderError

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class AiProviderResult:
    payload: Any = None
    provider: str | None = None
    model: str | None = None
    fallback_used: bool = False
    error_type: str | None = None
    upstream_status_code: int | None = None


class AiProviderRouter:
    """Select configured providers; metadata belongs to each individual call."""

    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._providers = {
            "openai": OpenAiTextProvider(settings),
            "gemini": GeminiProvider(settings),
        }

    async def _execute(self, *, feature: str, method: str,
                       provider: AiProviderName | None = None, **kwargs: Any) -> AiProviderResult:
        policy = self._settings.provider_for_feature(feature)
        if provider is not None:
            if feature != "coach":
                raise ValueError("Provider selection is only supported for Coach.")
            # An explicit choice must not silently switch to another provider.
            candidates = [provider]
        else:
            candidates = [policy.primary] + ([policy.fallback] if policy.fallback else [])
        error_type = "unconfigured"
        upstream_status_code = None
        attempted_provider = None
        attempted_model = None
        # The deadline covers all attempts, not a new full timeout per provider.
        try:
            async with asyncio.timeout(self._settings.request_timeout_seconds):
                for index, name in enumerate(candidates):
                    attempted_provider = name
                    attempted_model = self._settings.model_for_provider(name)
                    upstream_status_code = None
                    client = self._providers[name]
                    if not client.is_configured:
                        error_type = "unconfigured"
                        continue
                    model = attempted_model
                    try:
                        payload = await getattr(client, method)(model=model, **kwargs)
                    except AiProviderError as exc:
                        error_type = exc.error_type
                        upstream_status_code = exc.upstream_status_code
                        logger.warning(
                            "AI provider failed: feature=%s provider=%s model=%s error=%s upstream_status=%s",
                            feature, name, model, error_type, upstream_status_code,
                            extra={"feature": feature, "provider": name, "model": model,
                                   "error_type": error_type, "upstream_status_code": upstream_status_code},
                        )
                        continue
                    except Exception as exc:  # noqa: BLE001 - SDK failures must degrade safely
                        error_type = type(exc).__name__
                        logger.warning("AI provider failed: feature=%s provider=%s error=%s",
                                       feature, name, error_type)
                        continue
                    valid = (isinstance(payload, str) and bool(payload.strip())
                             if method == "complete" else isinstance(payload, dict))
                    if valid:
                        logger.info("AI request completed: feature=%s provider=%s model=%s fallback=%s",
                                    feature, name, model, index > 0)
                        return AiProviderResult(payload, name, model, index > 0)
                    error_type = "invalid_response"
        except TimeoutError:
            error_type = "timeout"
            upstream_status_code = None
        logger.warning("AI request unavailable: feature=%s error=%s", feature, error_type,
                       extra={"feature": feature, "provider": attempted_provider, "model": attempted_model,
                              "error_type": error_type, "upstream_status_code": upstream_status_code})
        return AiProviderResult(error_type=error_type, upstream_status_code=upstream_status_code)

    async def complete_result(self, *, feature: str, system_prompt: str,
                              user_prompt: str,
                              provider: AiProviderName | None = None) -> AiProviderResult:
        return await self._execute(feature=feature, method="complete",
                                   system_prompt=system_prompt, user_prompt=user_prompt,
                                   provider=provider)

    async def complete_json_result(self, *, feature: str, system_prompt: str,
                                   user_prompt: str) -> AiProviderResult:
        return await self._execute(feature=feature, method="complete_json",
                                   system_prompt=system_prompt, user_prompt=user_prompt)

    async def analyze_image_json_result(self, *, feature: str, system_prompt: str,
                                       user_prompt: str, image_base64: str,
                                       mime_type: str = "image/jpeg") -> AiProviderResult:
        return await self._execute(feature=feature, method="analyze_image_json",
                                   system_prompt=system_prompt, user_prompt=user_prompt,
                                   image_base64=image_base64, mime_type=mime_type)

    # Keep payload-only helpers for existing internal integrations.
    async def complete(self, **kwargs: Any) -> str | None:
        return (await self.complete_result(**kwargs)).payload

    async def complete_json(self, **kwargs: Any) -> dict | None:
        return (await self.complete_json_result(**kwargs)).payload

    async def analyze_image_json(self, **kwargs: Any) -> dict | None:
        return (await self.analyze_image_json_result(**kwargs)).payload

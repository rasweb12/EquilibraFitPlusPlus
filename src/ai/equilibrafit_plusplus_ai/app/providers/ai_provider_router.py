
import logging
from typing import Any, Literal

from app.core.config import Settings
from app.providers.openai_provider import OpenAiTextProvider
from app.providers.gemini_provider import GeminiProvider

logger = logging.getLogger(__name__)

ProviderName = Literal["openai", "gemini"]


class AiProviderRouter:
    """Select AI providers by feature and handle optional fallback."""

    FEATURE_FIELDS = {
        "coach": "coach_provider",
        "workouts": "workouts_provider",
        "plans": "plans_provider",
        "meals": "meals_provider",
        "meal_text": "meal_text_provider",
        "labels": "labels_provider",
    }

    def __init__(self, settings: Settings) -> None:
        self._settings = settings

        self._providers = {
            "openai": OpenAiTextProvider(settings),
            "gemini": GeminiProvider(settings),
        }

        self.last_provider: str | None = None
        self.last_model: str | None = None
        self.fallback_used: bool = False

    def _primary_for(self, feature: str) -> ProviderName:
        default = getattr(self._settings, "provider", "openai")

        field = self.FEATURE_FIELDS.get(feature)

        selected = (
            getattr(self._settings, field, default)
            if field
            else default
        )

        if selected not in self._providers:
            raise ValueError(
                f"Invalid AI provider for feature: {feature}"
            )

        return selected

    def _fallback_for(
        self,
        primary: ProviderName,
    ) -> ProviderName | None:

        enabled = getattr(
            self._settings,
            "fallback_enabled",
            False,
        )

        if not enabled:
            return None

        configured = getattr(
            self._settings,
            "fallback_provider",
            None,
        )

        # A global fallback can be configured explicitly.
        if configured in self._providers:
            if configured != primary:
                return configured

            # When both providers are configured,
            # automatically choose the other provider.
            return (
                "gemini"
                if primary == "openai"
                else "openai"
            )

        # No configured fallback: use the other provider.
        return (
            "gemini"
            if primary == "openai"
            else "openai"
        )

    def _model_for(self, provider: ProviderName) -> str:
        if provider == "gemini":
            return getattr(
                self._settings,
                "gemini_model",
                "gemini-2.5-flash",
            )

        return self._settings.openai_model

    async def _execute(
        self,
        *,
        feature: str,
        method: str,
        **kwargs: Any,
    ) -> Any:
        """Execute a provider operation with optional fallback."""

        self.last_provider = None
        self.last_model = None
        self.fallback_used = False

        primary = self._primary_for(feature)
        fallback = self._fallback_for(primary)

        candidates = [primary]

        if fallback and fallback != primary:
            candidates.append(fallback)

        for index, name in enumerate(candidates):
            provider = self._providers[name]

            if not provider.is_configured:
                logger.warning(
                    "AI provider unavailable: feature=%s provider=%s",
                    feature,
                    name,
                )
                continue

            model = self._model_for(name)

            try:
                operation = getattr(provider, method)

                result = await operation(
                    model=model,
                    **kwargs,
                )

            except Exception as exc:
                logger.warning(
                    "AI provider exception: feature=%s "
                    "provider=%s error=%s",
                    feature,
                    name,
                    type(exc).__name__,
                )
                result = None

            if result is not None:
                self.last_provider = name
                self.last_model = model
                self.fallback_used = index > 0

                logger.info(
                    "AI request completed: feature=%s "
                    "provider=%s model=%s fallback=%s",
                    feature,
                    name,
                    model,
                    self.fallback_used,
                )

                return result

            logger.warning(
                "AI provider returned no result: "
                "feature=%s provider=%s",
                feature,
                name,
            )

        logger.error(
            "All AI providers failed: feature=%s",
            feature,
        )

        return None

    async def complete(
        self,
        *,
        feature: str,
        system_prompt: str,
        user_prompt: str,
    ) -> str | None:
        """Generate a text response."""

        return await self._execute(
            feature=feature,
            method="complete",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )

    async def complete_json(
        self,
        *,
        feature: str,
        system_prompt: str,
        user_prompt: str,
    ) -> dict | None:
        """Generate a JSON response."""

        return await self._execute(
            feature=feature,
            method="complete_json",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )

    async def analyze_image_json(
        self,
        *,
        feature: str,
        system_prompt: str,
        user_prompt: str,
        image_base64: str,
        mime_type: str = "image/jpeg",
    ) -> dict | None:
        """Analyze an image and return JSON."""

        return await self._execute(
            feature=feature,
            method="analyze_image_json",
            system_prompt=system_prompt,
            user_prompt=user_prompt,
            image_base64=image_base64,
            mime_type=mime_type,
        )

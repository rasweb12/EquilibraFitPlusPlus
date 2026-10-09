
from functools import lru_cache
from typing import Literal

from pydantic import BaseModel, Field, model_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

AiProviderName = Literal["openai", "gemini"]


class AiCostPolicy(BaseModel):
    """Runtime limits used to control AI costs."""

    max_context_tokens: int = Field(default=6000, ge=1000, le=20000)
    max_retrieved_chunks: int = Field(default=4, ge=0, le=12)
    max_output_tokens: int = Field(default=1200, ge=200, le=4000)

    # Mantidos para compatibilidade.
    preferred_model: str = "gpt-4.1-mini"
    fallback_model: str = "equilibrafit-rules-v1"

    # Novas configurações multiprovedor.
    preferred_provider: AiProviderName = "openai"
    fallback_provider: AiProviderName | None = None
    fallback_enabled: bool = False

    enable_rag: bool = True
    enable_memory: bool = True


class AiFeaturePolicy(BaseModel):
    """Provider selection for an individual AI feature."""

    primary: AiProviderName = "openai"
    fallback: AiProviderName | None = None


class Settings(BaseSettings):
    """Runtime configuration loaded from environment variables."""

    model_config = SettingsConfigDict(
        env_prefix="EQUILIBRAFIT_AI_",
        env_file=".env",
        extra="ignore",
    )

    # --------------------------------------------------
    # Application
    # --------------------------------------------------

    environment: str = "Development"
    log_level: str = "INFO"
    enable_docs: bool = True

    # Chave interna usada pela API .NET.
    api_key: str | None = None

    # --------------------------------------------------
    # OpenAI
    # --------------------------------------------------

    openai_api_key: str | None = None
    openai_model: str = "gpt-4.1-mini"

    # --------------------------------------------------
    # Google Gemini
    # --------------------------------------------------

    gemini_api_key: str | None = None
    gemini_model: str = "gemini-2.5-flash"

    # --------------------------------------------------
    # Provider routing
    # --------------------------------------------------

    provider: AiProviderName = "openai"

    fallback_enabled: bool = False
    fallback_provider: AiProviderName | None = None

    # Provider selection by feature.
    # Defaults preserve OpenAI behavior unless
    # a feature is explicitly configured otherwise.

    coach_provider: AiProviderName = "openai"
    workouts_provider: AiProviderName = "openai"
    plans_provider: AiProviderName = "openai"
    meals_provider: AiProviderName = "openai"
    meal_text_provider: AiProviderName = "openai"
    labels_provider: AiProviderName = "openai"

    # --------------------------------------------------
    # Request configuration
    # --------------------------------------------------

    request_timeout_seconds: int = Field(
        default=30,
        ge=5,
        le=120,
    )

    yolo_model_path: str | None = None
    default_language: str = "pt-BR"

    # --------------------------------------------------
    # AI cost policy
    # --------------------------------------------------

    ai_max_context_tokens: int = Field(
        default=6000,
        ge=1000,
        le=20000,
    )

    ai_max_retrieved_chunks: int = Field(
        default=4,
        ge=0,
        le=12,
    )

    ai_max_output_tokens: int = Field(
        default=1200,
        ge=200,
        le=4000,
    )

    ai_enable_rag: bool = True
    ai_enable_memory: bool = True

    # --------------------------------------------------
    # Validation
    # --------------------------------------------------

    @model_validator(mode="after")
    def validate_provider_configuration(self):
        """Validate the fallback configuration."""

        if self.fallback_enabled and self.fallback_provider is None:
            raise ValueError("Configure FALLBACK_PROVIDER when FALLBACK_ENABLED=true.")

        return self

    # --------------------------------------------------
    # Provider helpers
    # --------------------------------------------------

    def model_for_provider(
        self,
        provider: AiProviderName,
    ) -> str:
        """Return the configured model for a provider."""

        if provider == "gemini":
            return self.gemini_model

        return self.openai_model

    def api_key_for_provider(
        self,
        provider: AiProviderName,
    ) -> str | None:
        """Return the provider API key."""

        if provider == "gemini":
            return self.gemini_api_key

        return self.openai_api_key

    def is_provider_configured(
        self,
        provider: AiProviderName,
    ) -> bool:
        """Check whether a provider has credentials."""

        return bool(self.api_key_for_provider(provider))

    def provider_for_feature(
        self,
        feature: str,
    ) -> AiFeaturePolicy:
        """Resolve provider policy for an AI feature."""

        feature_providers = {
            "coach": self.coach_provider,
            "workouts": self.workouts_provider,
            "plans": self.plans_provider,
            "meals": self.meals_provider,
            "meal_text": self.meal_text_provider,
            "labels": self.labels_provider,
        }

        primary = feature_providers.get(
            feature,
            self.provider,
        )

        fallback = None

        if self.fallback_enabled:
            candidate = self.fallback_provider

            if candidate != primary:
                fallback = candidate

        return AiFeaturePolicy(
            primary=primary,
            fallback=fallback,
        )

    # --------------------------------------------------
    # Cost policy
    # --------------------------------------------------

    @property
    def ai_cost_policy(self) -> AiCostPolicy:
        """Return consolidated AI cost and feature policy."""

        return AiCostPolicy(
            max_context_tokens=self.ai_max_context_tokens,
            max_retrieved_chunks=self.ai_max_retrieved_chunks,
            max_output_tokens=self.ai_max_output_tokens,
            preferred_model=self.model_for_provider(
                self.provider
            ),
            preferred_provider=self.provider,
            fallback_provider=(
                self.fallback_provider
                if self.fallback_enabled
                and self.fallback_provider != self.provider
                else None
            ),
            fallback_enabled=self.fallback_enabled,
            enable_rag=self.ai_enable_rag,
            enable_memory=self.ai_enable_memory,
        )


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    """Return cached settings."""
    return Settings()

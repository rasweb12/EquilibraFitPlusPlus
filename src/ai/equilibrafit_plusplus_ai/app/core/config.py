from functools import lru_cache

from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class AiCostPolicy(BaseModel):
    """Runtime limits used to control AI costs."""

    max_context_tokens: int = Field(default=6000, ge=1000, le=20000)
    max_retrieved_chunks: int = Field(default=4, ge=0, le=12)
    max_output_tokens: int = Field(default=1200, ge=200, le=4000)
    preferred_model: str = "gpt-4.1-mini"
    fallback_model: str = "equilibrafit-rules-v1"
    enable_rag: bool = True
    enable_memory: bool = True


class Settings(BaseSettings):
    """Runtime configuration loaded from environment variables."""

    model_config = SettingsConfigDict(env_prefix="EQUILIBRAFIT_AI_", env_file=".env", extra="ignore")

    environment: str = "Development"
    log_level: str = "INFO"
    enable_docs: bool = True
    api_key: str | None = None
    openai_api_key: str | None = None
    openai_model: str = "gpt-4.1-mini"
    request_timeout_seconds: int = Field(default=30, ge=5, le=120)
    yolo_model_path: str | None = None
    default_language: str = "pt-BR"
    ai_max_context_tokens: int = Field(default=6000, ge=1000, le=20000)
    ai_max_retrieved_chunks: int = Field(default=4, ge=0, le=12)
    ai_max_output_tokens: int = Field(default=1200, ge=200, le=4000)
    ai_enable_rag: bool = True
    ai_enable_memory: bool = True

    @property
    def ai_cost_policy(self) -> AiCostPolicy:
        """Return consolidated AI cost and feature policy."""
        return AiCostPolicy(
            max_context_tokens=self.ai_max_context_tokens,
            max_retrieved_chunks=self.ai_max_retrieved_chunks,
            max_output_tokens=self.ai_max_output_tokens,
            preferred_model=self.openai_model,
            enable_rag=self.ai_enable_rag,
            enable_memory=self.ai_enable_memory,
        )


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    """Return cached settings."""
    return Settings()

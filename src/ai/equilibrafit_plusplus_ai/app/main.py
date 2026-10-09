
import logging
from uuid import UUID, uuid4

from fastapi import FastAPI, Request

from app.api.v1.router import api_router
from app.core.config import get_settings
from app.core.logging import configure_logging, correlation_id


logger = logging.getLogger(__name__)


def create_app() -> FastAPI:
    """Create the EquilibraFit++ AI application."""

    settings = get_settings()

    # --------------------------------------------------
    # Production security
    # --------------------------------------------------

    if (
        settings.environment.casefold() == "production"
        and not settings.api_key
    ):
        raise RuntimeError(
            "EQUILIBRAFIT_AI_API_KEY must be configured in Production."
        )

    configure_logging(settings.log_level)

    # --------------------------------------------------
    # AI provider configuration
    # --------------------------------------------------

    default_provider = settings.provider
    default_model = settings.model_for_provider(default_provider)

    openai_configured = settings.is_provider_configured("openai")
    gemini_configured = settings.is_provider_configured("gemini")

    logger.info(
        "AI gateway initialized: provider=%s model=%s "
        "openai_configured=%s gemini_configured=%s "
        "fallback_enabled=%s",
        default_provider,
        default_model,
        openai_configured,
        gemini_configured,
        settings.fallback_enabled,
    )

    # --------------------------------------------------
    # FastAPI application
    # --------------------------------------------------

    app = FastAPI(
        title="EquilibraFit++ AI",
        version="0.2.0",
        description=(
            "Internal multi-provider AI gateway supporting "
            "OpenAI and Google Gemini for coaching, workouts, "
            "meal analysis, nutrition labels and food plans."
        ),
        docs_url="/docs" if settings.enable_docs else None,
        redoc_url="/redoc" if settings.enable_docs else None,
    )

    # --------------------------------------------------
    # Correlation ID middleware
    # --------------------------------------------------

    @app.middleware("http")
    async def correlate(request: Request, call_next):
        try:
            identifier = str(
                UUID(request.headers.get("X-Correlation-ID", ""))
            )
        except (ValueError, TypeError):
            identifier = str(uuid4())

        token = correlation_id.set(identifier)

        try:
            response = await call_next(request)

            response.headers["X-Correlation-ID"] = identifier

            logging.getLogger("requests").info(
                "%s %s %s",
                request.method,
                request.url.path,
                response.status_code,
            )

            return response

        finally:
            correlation_id.reset(token)

    # --------------------------------------------------
    # Health check
    # --------------------------------------------------

    @app.get("/health", tags=["system"])
    async def health() -> dict[str, str]:
        """
        Report service health and configured AI provider.

        This endpoint does not make external API calls.
        """

        provider = settings.provider

        return {
            "status": "Healthy",
            "service": "equilibrafit-plusplus-ai",
            "provider": provider,
            "model": settings.model_for_provider(provider),
        }

    # --------------------------------------------------
    # API routes
    # --------------------------------------------------

    app.include_router(api_router)

    return app


app = create_app()

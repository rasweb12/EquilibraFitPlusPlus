
import logging
import os
import re
from contextlib import asynccontextmanager
from time import perf_counter
from uuid import UUID, uuid4

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

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
    commit = os.getenv("RENDER_GIT_COMMIT", "")
    revision = commit[:12] if re.fullmatch(r"[0-9a-fA-F]{40,64}", commit) else "unknown"
    process = {"process_id": os.getpid(), "revision": revision}

    @asynccontextmanager
    async def lifespan(application: FastAPI):
        logger.info("service.started", extra=process)
        try:
            yield
        finally:
            logger.info("service.stopped", extra=process)

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
        lifespan=lifespan,
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
        started = perf_counter()
        fields = {"method": request.method, "path": request.url.path[:256], **process}
        requests_logger = logging.getLogger("requests")
        requests_logger.info("request.started", extra=fields)

        try:
            try:
                response = await call_next(request)
            except Exception as exception:  # noqa: BLE001 -- safe HTTP boundary, never log request or exception text
                requests_logger.error("request.failed", extra={**fields, "error_type": type(exception).__name__})
                response = JSONResponse(status_code=500, content={
                    "code": "ai.internal_error",
                    "message": "AI temporarily unavailable.",
                    "correlation_id": identifier,
                })

            response.headers["X-Correlation-ID"] = identifier

            requests_logger.info("request.completed", extra={
                **fields,
                "status_code": response.status_code,
                "duration_ms": round((perf_counter() - started) * 1000, 2),
            })

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
            "revision": revision,
            "provider": provider,
            "model": settings.model_for_provider(provider),
        }

    # --------------------------------------------------
    # API routes
    # --------------------------------------------------

    app.include_router(api_router)

    return app


app = create_app()

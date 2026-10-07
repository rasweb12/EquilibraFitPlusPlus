import logging
from uuid import UUID, uuid4

from fastapi import FastAPI, Request

from app.api.v1.router import api_router
from app.core.config import get_settings
from app.core.logging import configure_logging, correlation_id


def create_app() -> FastAPI:
    """Create the FastAPI application."""
    settings = get_settings()
    if settings.environment.casefold() == "production" and not settings.api_key:
        raise RuntimeError("EQUILIBRAFIT_AI_API_KEY must be configured in Production.")
    configure_logging(settings.log_level)

    app = FastAPI(
        title="EquilibraFit++ AI",
        version="0.1.0",
        description="Internal AI gateway for plans, workouts, meals, labels and coach responses.",
        docs_url="/docs" if settings.enable_docs else None,
        redoc_url="/redoc" if settings.enable_docs else None,
    )

    @app.middleware("http")
    async def correlate(request: Request, call_next):
        try:
            identifier = str(UUID(request.headers.get("X-Correlation-ID", "")))
        except ValueError:
            identifier = str(uuid4())
        token = correlation_id.set(identifier)
        try:
            response = await call_next(request)
            response.headers["X-Correlation-ID"] = identifier
            logging.getLogger("requests").info("%s %s %s", request.method, request.url.path, response.status_code)
            return response
        finally:
            correlation_id.reset(token)

    @app.get("/health", tags=["system"])
    async def health() -> dict[str, str]:
        """Return service health."""
        return {"status": "Healthy", "service": "equilibrafit-plusplus-ai", "model": settings.openai_model}

    app.include_router(api_router)
    return app


app = create_app()

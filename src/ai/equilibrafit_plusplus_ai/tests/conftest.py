import pytest

from app.core.config import get_settings


@pytest.fixture(autouse=True)
def isolated_ai_environment(monkeypatch):
    # Real credentials from a developer shell must never activate paid unit tests.
    for key in ("API_KEY", "OPENAI_API_KEY", "GEMINI_API_KEY"):
        monkeypatch.setenv("EQUILIBRAFIT_AI_" + key, "")
    monkeypatch.setenv("EQUILIBRAFIT_AI_ENVIRONMENT", "Development")
    monkeypatch.setenv("EQUILIBRAFIT_AI_FALLBACK_ENABLED", "false")
    get_settings.cache_clear()
    yield
    get_settings.cache_clear()

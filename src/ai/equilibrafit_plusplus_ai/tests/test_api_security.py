import pytest
from fastapi.testclient import TestClient

from app.core.config import get_settings
from app.main import create_app


def test_health_does_not_require_api_key() -> None:
    client = TestClient(create_app())

    response = client.get("/health")

    assert response.status_code == 200
    assert response.json()["status"] == "Healthy"


def test_correlation_id_is_preserved_or_replaced_when_invalid() -> None:
    client = TestClient(create_app())
    identifier = "87622e2a-26a4-456c-847a-6f509019bdfe"
    assert client.get("/health", headers={"X-Correlation-ID": identifier}).headers["X-Correlation-ID"] == identifier
    assert client.get("/health", headers={"X-Correlation-ID": "untrusted"}).headers["X-Correlation-ID"] != "untrusted"


def test_production_refuses_to_start_without_internal_key(monkeypatch) -> None:
    get_settings.cache_clear()
    monkeypatch.setenv("EQUILIBRAFIT_AI_ENVIRONMENT", "Production")
    monkeypatch.delenv("EQUILIBRAFIT_AI_API_KEY", raising=False)
    with pytest.raises(RuntimeError, match="EQUILIBRAFIT_AI_API_KEY"):
        create_app()
    get_settings.cache_clear()


def test_internal_endpoint_requires_api_key_when_configured(monkeypatch) -> None:
    get_settings.cache_clear()
    monkeypatch.setenv("EQUILIBRAFIT_AI_API_KEY", "secret")
    client = TestClient(create_app())

    response = client.post("/api/v1/coach/message", json={"mensagem": "Oi"})

    assert response.status_code == 401

    get_settings.cache_clear()


def test_canonical_coach_chat_endpoint_uses_safe_fallback_without_openai_key() -> None:
    get_settings.cache_clear()
    client = TestClient(create_app())

    response = client.post("/api/v1/coach/chat", json={"mensagem": "Como ajustar meu jantar hoje?"})

    assert response.status_code == 200
    body = response.json()
    assert body["fallback_used"]
    assert "Sem problemas" in body["conteudo"]


def test_text_meal_estimation_endpoint_returns_hybrid_result_without_openai_key() -> None:
    get_settings.cache_clear()
    client = TestClient(create_app())

    response = client.post("/api/v1/meals/estimate-text", json={"description": "pão com ovo"})

    assert response.status_code == 200
    body = response.json()
    assert body["fallback_used"]
    assert body["items"]

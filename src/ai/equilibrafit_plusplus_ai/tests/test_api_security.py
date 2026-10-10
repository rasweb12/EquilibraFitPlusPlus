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


@pytest.mark.parametrize("provider", ["unknown", "", "OpenAI", "https://untrusted.test"])
def test_coach_rejects_invalid_provider_before_processing(provider):
    with TestClient(create_app()) as client:
        response = client.post("/api/v1/coach/chat", json={"mensagem": "Oi", "provider": provider})
    assert response.status_code == 422


@pytest.mark.parametrize("provider", ["openai", "gemini"])
def test_coach_selected_provider_uses_safe_fallback_without_keys(provider):
    with TestClient(create_app()) as client:
        response = client.post("/api/v1/coach/chat", json={"mensagem": "Como ajustar meu jantar?", "provider": provider})
    assert response.status_code == 200
    assert response.json()["fallback_used"]
    assert response.json()["modelo"] == "equilibrafit-coach-rules-v1"


def test_text_meal_estimation_endpoint_returns_hybrid_result_without_openai_key() -> None:
    get_settings.cache_clear()
    client = TestClient(create_app())

    response = client.post("/api/v1/meals/estimate-text", json={"description": "pão com ovo"})

    assert response.status_code == 200
    body = response.json()
    assert body["fallback_used"]
    assert body["items"]


def test_unexpected_failure_returns_safe_json_and_keeps_correlation() -> None:
    app = create_app()

    @app.get("/test-failure")
    async def failing_request():
        raise RuntimeError("private-user-and-key")

    identifier = "87622e2a-26a4-456c-847a-6f509019bdfe"
    with TestClient(app) as client:
        response = client.get("/test-failure", headers={"X-Correlation-ID": identifier})
    assert response.status_code == 500
    assert response.headers["X-Correlation-ID"] == identifier
    assert response.json()["correlation_id"] == identifier
    assert response.json()["code"] == "ai.internal_error"
    assert "private-user-and-key" not in response.text


def test_request_logs_include_duration_and_correlation_without_exception_text(monkeypatch) -> None:
    import json
    import logging
    from io import StringIO

    from app.core.logging import JsonFormatter

    app = create_app()

    @app.get("/test-failure")
    async def failing_request():
        raise RuntimeError("private-user-and-key")

    output = StringIO()
    handler = logging.StreamHandler(output)
    handler.setFormatter(JsonFormatter())
    monkeypatch.setattr(logging.getLogger(), "handlers", [handler])
    identifier = "87622e2a-26a4-456c-847a-6f509019bdfe"
    with TestClient(app) as client:
        client.get("/test-failure", headers={"X-Correlation-ID": identifier})
    records = [json.loads(line) for line in output.getvalue().splitlines()]
    requests = [record for record in records if record["logger"] == "requests"]
    assert [record["message"] for record in requests] == ["request.started", "request.failed", "request.completed"]
    assert all(record["correlation_id"] == identifier for record in requests)
    assert requests[1]["error_type"] == "RuntimeError"
    assert requests[-1]["duration_ms"] >= 0
    assert requests[-1]["status_code"] == 500
    assert "private-user-and-key" not in output.getvalue()
    assert any(record["message"] == "service.started" for record in records)
    assert any(record["message"] == "service.stopped" for record in records)


def test_health_reports_only_validated_build_revision(monkeypatch) -> None:
    monkeypatch.setenv("RENDER_GIT_COMMIT", "a" * 40)
    with TestClient(create_app()) as client:
        assert client.get("/health").json()["revision"] == "a" * 12
    monkeypatch.setenv("RENDER_GIT_COMMIT", "private-user-and-key")
    with TestClient(create_app()) as client:
        assert client.get("/health").json()["revision"] == "unknown"

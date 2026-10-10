import asyncio
import io
import json
import logging
from types import SimpleNamespace
from unittest.mock import AsyncMock

import httpx
import pytest
from google.genai import errors, types

from app.core.config import Settings
from app.core.logging import JsonFormatter, correlation_id
from app.providers.ai_provider_router import AiProviderRouter
from app.providers.gemini_provider import GeminiProvider
from app.providers.provider_errors import AiProviderError
from app.schemas.coach import CoachMessageRequest
from app.schemas.meals import MealRecognizeRequest
from app.services.coach_service import CoachService
from app.services.meal_recognition_service import MealRecognitionService

PRIVATE_MESSAGE = "private-key-and-user-content-must-not-be-logged"


def gemini_with_mock(settings, *, payload="Resposta segura", exception=None):
    provider = GeminiProvider(settings)
    provider._types = types
    operation = AsyncMock(return_value=SimpleNamespace(text=payload), side_effect=exception)
    provider._client = SimpleNamespace(aio=SimpleNamespace(models=SimpleNamespace(generate_content=operation)))
    return provider, operation


def router_with_gemini(settings, *, payload="Resposta segura", exception=None):
    router = AiProviderRouter(settings)
    provider, operation = gemini_with_mock(settings, payload=payload, exception=exception)
    other = AsyncMock(return_value="Must not be called")
    router._providers = {
        "gemini": provider,
        "openai": SimpleNamespace(is_configured=True, complete=other, complete_json=other, analyze_image_json=other),
    }
    return router, operation, other


async def invoke(router, method):
    arguments = {"system_prompt": "s", "user_prompt": "u"}
    if method == "complete":
        return await router.complete_result(feature="coach", provider="gemini", **arguments)
    if method == "complete_json":
        return await router.complete_json_result(feature="plans", **arguments)
    return await router.analyze_image_json_result(feature="meals", image_base64="AQIDBA==", **arguments)


@pytest.mark.asyncio
@pytest.mark.parametrize("method", ["complete", "complete_json", "analyze_image_json"])
@pytest.mark.parametrize("code,category", [
    (400, "invalid_request"), (401, "authentication_failed"), (403, "permission_denied"),
    (404, "model_not_found"), (429, "rate_limit"), (500, "provider_unavailable"),
    (502, "provider_unavailable"), (504, "timeout"),
])
async def test_sdk_failures_keep_safe_status_and_do_not_switch_provider(method, code, category, caplog):
    error_class = errors.ClientError if code < 500 else errors.ServerError
    exception = error_class(code, {"error": {"message": PRIVATE_MESSAGE}})
    router, operation, other = router_with_gemini(Settings(_env_file=None), exception=exception)
    result = await invoke(router, method)
    assert result.payload is None and result.model is None
    assert result.error_type == category
    assert result.upstream_status_code == code
    operation.assert_awaited_once()
    other.assert_not_awaited()
    assert PRIVATE_MESSAGE not in caplog.text
    logs = [json.loads(JsonFormatter().format(record)) for record in caplog.records]
    failure = next(record for record in logs if record.get("upstream_status_code") == code)
    assert failure["provider"] == "gemini"
    assert failure["model"] == router._settings.gemini_model
    assert failure["error_type"] == category
    assert PRIVATE_MESSAGE not in json.dumps(logs)


@pytest.mark.asyncio
@pytest.mark.parametrize("method", ["complete", "complete_json", "analyze_image_json"])
@pytest.mark.parametrize("exception,category", [
    (TimeoutError(PRIVATE_MESSAGE), "timeout"),
    (httpx.ReadTimeout(PRIVATE_MESSAGE), "timeout"),
    (httpx.ConnectError(PRIVATE_MESSAGE), "transport_error"),
    (RuntimeError(PRIVATE_MESSAGE), "provider_error"),
])
async def test_transport_errors_remain_safe_without_sdk_message(method, exception, category, caplog):
    router, operation, other = router_with_gemini(Settings(_env_file=None), exception=exception)
    result = await invoke(router, method)
    assert result.error_type == category and result.upstream_status_code is None
    assert result.payload is None
    operation.assert_awaited_once()
    other.assert_not_awaited()
    assert PRIVATE_MESSAGE not in caplog.text


@pytest.mark.asyncio
@pytest.mark.parametrize("method", ["complete_json", "analyze_image_json"])
async def test_invalid_json_is_not_reported_as_an_http_error(method):
    router, _, other = router_with_gemini(Settings(_env_file=None), payload="not JSON")
    result = await invoke(router, method)
    assert result.error_type == "invalid_response" and result.upstream_status_code is None
    assert result.payload is None
    other.assert_not_awaited()


@pytest.mark.asyncio
async def test_gemini_caller_cancellation_is_not_converted_to_fallback():
    router, _, other = router_with_gemini(Settings(_env_file=None), exception=asyncio.CancelledError())
    with pytest.raises(asyncio.CancelledError):
        await invoke(router, "complete")
    other.assert_not_awaited()


@pytest.mark.asyncio
@pytest.mark.parametrize("image,mime_type", [("not base64", "image/png"), ("", "image/png"), ("AQID", "text/plain")])
async def test_invalid_image_does_not_make_a_provider_call(image, mime_type):
    provider, operation = gemini_with_mock(Settings(_env_file=None))
    result = await provider.analyze_image_json(
        system_prompt="s", user_prompt="u", image_base64=image, mime_type=mime_type,
    )
    assert result is None
    operation.assert_not_awaited()


@pytest.mark.asyncio
async def test_concurrent_failures_do_not_leak_status_or_correlation_to_success():
    router, operation, _ = router_with_gemini(Settings(_env_file=None))

    async def respond(**kwargs):
        await asyncio.sleep(0.01)
        if kwargs["contents"] == "missing":
            raise errors.ClientError(404, {"error": {"message": PRIVATE_MESSAGE}})
        return SimpleNamespace(text="Resposta segura")

    async def call(message, request_id):
        token = correlation_id.set(request_id)
        try:
            return await router.complete_result(
                feature="coach", provider="gemini", system_prompt="s", user_prompt=message,
            )
        finally:
            correlation_id.reset(token)

    operation.side_effect = respond
    output = io.StringIO()
    handler = logging.StreamHandler(output)
    handler.setFormatter(JsonFormatter())
    logger = logging.getLogger("app.providers.ai_provider_router")
    logger.addHandler(handler)
    try:
        failure, success = await asyncio.gather(call("missing", "failure-id"), call("ok", "success-id"))
    finally:
        logger.removeHandler(handler)
    assert failure.error_type == "model_not_found" and failure.upstream_status_code == 404
    assert success.payload == "Resposta segura" and success.upstream_status_code is None
    assert success.error_type is None and success.model == router._settings.gemini_model
    logs = [json.loads(line) for line in output.getvalue().splitlines()]
    assert logs
    assert all(record["correlation_id"] == "failure-id" for record in logs if record.get("upstream_status_code") == 404)


@pytest.mark.asyncio
@pytest.mark.parametrize("method", ["complete", "complete_json", "analyze_image_json"])
async def test_available_model_returns_provider_content_not_local_support(method):
    settings = Settings(_env_file=None, gemini_model="gemini-3.5-flash-lite")
    payload = "Resposta segura" if method == "complete" else '{"ok": true}'
    router, operation, other = router_with_gemini(settings, payload=payload)
    result = await invoke(router, method)
    assert result.payload == ("Resposta segura" if method == "complete" else {"ok": True})
    assert result.provider == "gemini" and result.model == settings.gemini_model
    assert not result.fallback_used and result.error_type is None
    assert operation.await_args.kwargs["model"] == settings.gemini_model
    other.assert_not_awaited()


@pytest.mark.asyncio
async def test_coach_404_remains_labeled_local_support_not_a_gemini_answer():
    settings = Settings(_env_file=None)
    service = CoachService(settings)
    service._provider, _, other = router_with_gemini(
        settings, exception=errors.ClientError(404, {"error": {"message": PRIVATE_MESSAGE}}),
    )
    response = await service.reply(CoachMessageRequest(mensagem="Como adaptar?", provider="gemini"))
    assert response.fallback_used and response.modelo == "equilibrafit-coach-rules-v1"
    assert PRIVATE_MESSAGE not in response.conteudo
    other.assert_not_awaited()


@pytest.mark.asyncio
async def test_image_404_keeps_manual_nutrition_review_without_invented_measurements():
    settings = Settings(_env_file=None, yolo_model_path=None)
    service = MealRecognitionService(settings)
    service._provider, _, other = router_with_gemini(
        settings, exception=errors.ClientError(404, {"error": {"message": PRIVATE_MESSAGE}}),
    )
    response = await service.recognize(MealRecognizeRequest(image_base64="AQIDBA=="))
    assert response.fallback_used and response.requires_user_review
    assert response.confidence == 0 and response.items == []
    other.assert_not_awaited()


def test_current_model_default_and_explicit_environment_override(monkeypatch):
    monkeypatch.delenv("EQUILIBRAFIT_AI_GEMINI_MODEL", raising=False)
    assert Settings(_env_file=None).gemini_model == "gemini-3.5-flash-lite"
    monkeypatch.setenv("EQUILIBRAFIT_AI_GEMINI_MODEL", "gemini-2.5-flash")
    assert Settings(_env_file=None).gemini_model == "gemini-2.5-flash"


def test_provider_error_does_not_copy_sdk_exception_details():
    exception = errors.ClientError(404, {"error": {"message": PRIVATE_MESSAGE}})
    safe = AiProviderError.from_exception(exception)
    assert str(safe) == "model_not_found"
    assert not hasattr(safe, "details") and not hasattr(safe, "message")

import asyncio
from types import SimpleNamespace
from unittest.mock import AsyncMock

import pytest

from app.core.config import Settings
from app.providers.ai_provider_router import AiProviderRouter
from app.providers.gemini_provider import GeminiProvider
from app.providers.openai_provider import OpenAiTextProvider, _extract_json_object


def fake_provider(payload=None, *, configured=True, exception=None):
    operation = AsyncMock(return_value=payload, side_effect=exception)
    return SimpleNamespace(is_configured=configured, complete=operation,
                           complete_json=operation, analyze_image_json=operation)


@pytest.mark.asyncio
@pytest.mark.parametrize("feature,provider", [
    ("coach", "openai"), ("workouts", "openai"), ("plans", "gemini"),
    ("meals", "gemini"), ("meal_text", "gemini"), ("labels", "gemini"),
])
async def test_each_feature_uses_its_configured_provider(feature, provider):
    router = AiProviderRouter(Settings(plans_provider="gemini", meals_provider="gemini",
                                      meal_text_provider="gemini", labels_provider="gemini"))
    router._providers = {name: fake_provider({"ok": name}) for name in ("openai", "gemini")}
    result = await router.complete_json_result(feature=feature, system_prompt="s", user_prompt="u")
    assert result.provider == provider
    assert result.model == router._settings.model_for_provider(provider)
    assert result.payload == {"ok": provider}
    assert not result.fallback_used
    router._providers[provider].complete_json.assert_awaited_once_with(
        model=result.model, system_prompt="s", user_prompt="u")
    other = "gemini" if provider == "openai" else "openai"
    router._providers[other].complete_json.assert_not_awaited()


@pytest.mark.asyncio
@pytest.mark.parametrize("configured", [True, False])
async def test_disabled_fallback_never_calls_other_provider(configured):
    router = AiProviderRouter(Settings(fallback_enabled=False, fallback_provider="gemini"))
    router._providers = {"openai": fake_provider(configured=configured), "gemini": fake_provider({})}
    result = await router.complete_json_result(feature="coach", system_prompt="s", user_prompt="u")
    assert result.payload is None
    assert result.error_type == ("invalid_response" if configured else "unconfigured")
    router._providers["gemini"].complete_json.assert_not_awaited()


@pytest.mark.asyncio
async def test_explicit_fallback_reports_actual_model():
    router = AiProviderRouter(Settings(fallback_enabled=True, fallback_provider="gemini"))
    router._providers = {"openai": fake_provider(), "gemini": fake_provider({"ok": True})}
    result = await router.complete_json_result(feature="coach", system_prompt="s", user_prompt="u")
    assert result.fallback_used and result.provider == "gemini" and result.model == "gemini-2.5-flash"


@pytest.mark.asyncio
async def test_fallback_equal_to_primary_does_not_choose_another_provider():
    router = AiProviderRouter(Settings(fallback_enabled=True, fallback_provider="openai"))
    router._providers = {"openai": fake_provider(), "gemini": fake_provider({})}
    await router.complete_json_result(feature="coach", system_prompt="s", user_prompt="u")
    router._providers["gemini"].complete_json.assert_not_awaited()


@pytest.mark.asyncio
async def test_concurrent_calls_keep_their_own_metadata():
    router = AiProviderRouter(Settings(plans_provider="gemini"))

    async def delayed(**kwargs):
        await asyncio.sleep(0.01)
        return {"model": kwargs["model"]}

    router._providers = {name: fake_provider() for name in ("openai", "gemini")}
    router._providers["openai"].complete_json.side_effect = delayed
    router._providers["gemini"].complete_json.return_value = {"model": "gemini"}
    results = await asyncio.gather(*[
        router.complete_json_result(feature=feature, system_prompt="s", user_prompt="u")
        for feature in ["coach", "plans"] * 12
    ])
    for index, result in enumerate(results):
        assert result.provider == ("openai" if index % 2 == 0 else "gemini")
        assert result.model == router._settings.model_for_provider(result.provider)
    router._providers["gemini"].complete_json.return_value = None
    failure = await router.complete_json_result(feature="plans", system_prompt="s", user_prompt="u")
    assert failure.payload is None and failure.model is None
    assert results[0].model == "gpt-4.1-mini"


@pytest.mark.asyncio
async def test_deadline_returns_safe_failure_without_automatic_fallback():
    settings = Settings()
    settings.request_timeout_seconds = 0.01
    router = AiProviderRouter(settings)
    async def slow(**kwargs):
        await asyncio.sleep(1)
    router._providers = {"openai": fake_provider(), "gemini": fake_provider({})}
    router._providers["openai"].complete_json.side_effect = slow
    result = await router.complete_json_result(feature="workouts", system_prompt="s", user_prompt="u")
    assert result.error_type == "timeout" and result.payload is None
    router._providers["gemini"].complete_json.assert_not_awaited()


@pytest.mark.asyncio
async def test_caller_cancellation_is_not_hidden():
    router = AiProviderRouter(Settings())
    router._providers = {"openai": fake_provider(exception=asyncio.CancelledError())}
    with pytest.raises(asyncio.CancelledError):
        await router.complete_result(feature="coach", system_prompt="s", user_prompt="u")


@pytest.mark.parametrize("text", ["not JSON", "[1,2]", "null", '{"broken":', ""])
def test_invalid_json_never_becomes_payload(text):
    assert _extract_json_object(text) is None
    assert GeminiProvider._parse_json(text) is None


@pytest.mark.asyncio
async def test_provider_exception_does_not_log_credentials(caplog):
    router = AiProviderRouter(Settings())
    router._providers = {"openai": fake_provider(exception=RuntimeError("test-secret-no-log"))}
    result = await router.complete_result(feature="coach", system_prompt="s", user_prompt="u")
    assert result.error_type == "RuntimeError"
    assert "test-secret-no-log" not in caplog.text


@pytest.mark.asyncio
async def test_openai_limits_output_and_uses_requested_model():
    provider = OpenAiTextProvider(Settings(ai_max_output_tokens=700))
    create = AsyncMock(return_value=SimpleNamespace(output_text='{"ok":true}'))
    provider._client = SimpleNamespace(responses=SimpleNamespace(create=create))
    assert await provider.complete_json(system_prompt="s", user_prompt="u", model="selected-model") == {"ok": True}
    assert create.await_args.kwargs["max_output_tokens"] == 700
    assert create.await_args.kwargs["model"] == "selected-model"


@pytest.mark.asyncio
async def test_gemini_image_passes_bytes_mime_and_json_config():
    from google.genai import types
    provider = GeminiProvider(Settings(ai_max_output_tokens=700))
    provider._types = types
    generate = AsyncMock(return_value=SimpleNamespace(text='{"calories": 12}'))
    provider._client = SimpleNamespace(aio=SimpleNamespace(models=SimpleNamespace(generate_content=generate)))
    result = await provider.analyze_image_json(system_prompt="s", user_prompt="u",
                                               image_base64="AQIDBA==", mime_type="image/png")
    assert result == {"calories": 12}
    kwargs = generate.await_args.kwargs
    assert kwargs["contents"][1].inline_data.data == b"\x01\x02\x03\x04"
    assert kwargs["contents"][1].inline_data.mime_type == "image/png"
    assert kwargs["config"].response_mime_type == "application/json"
    assert kwargs["config"].max_output_tokens == 700

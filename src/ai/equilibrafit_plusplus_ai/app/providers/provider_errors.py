import httpx


class AiProviderError(Exception):
    """Carry only safe failure metadata, never SDK messages or response bodies."""

    def __init__(self, error_type: str, upstream_status_code: int | None = None) -> None:
        super().__init__(error_type)
        self.error_type = error_type
        self.upstream_status_code = upstream_status_code

    @classmethod
    def from_exception(cls, exception: Exception) -> "AiProviderError":
        if isinstance(exception, (TimeoutError, httpx.TimeoutException)):
            return cls("timeout")
        if isinstance(exception, httpx.RequestError):
            return cls("transport_error")

        # Google SDK errors expose the HTTP status as code. Do not inspect details/message.
        code = getattr(exception, "code", None)
        if type(code) is not int or not 400 <= code <= 599:
            return cls("provider_error")
        categories = {
            400: "invalid_request",
            401: "authentication_failed",
            403: "permission_denied",
            404: "model_not_found",
            408: "timeout",
            429: "rate_limit",
            504: "timeout",
        }
        category = categories.get(code, "provider_unavailable" if code >= 500 else "provider_error")
        return cls(category, code)

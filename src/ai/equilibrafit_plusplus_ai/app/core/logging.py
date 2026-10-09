import json
import logging
from contextvars import ContextVar
from datetime import UTC, datetime

correlation_id: ContextVar[str | None] = ContextVar("correlation_id", default=None)


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        fields = {
            "timestamp": datetime.now(UTC).isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
            "correlation_id": correlation_id.get(),
        }
        for name in ("method", "path", "status_code", "duration_ms", "error_type", "process_id", "revision"):
            if hasattr(record, name):
                fields[name] = getattr(record, name)
        return json.dumps(fields)


def configure_logging(level: str) -> None:
    """Configure application logging without sensitive payloads."""
    handler = logging.StreamHandler()
    handler.setFormatter(JsonFormatter())
    logging.basicConfig(level=getattr(logging, level.upper(), logging.INFO), handlers=[handler], force=True)

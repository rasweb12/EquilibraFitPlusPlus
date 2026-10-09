import os
import socket
import subprocess
import sys
from pathlib import Path
from time import monotonic, sleep

import httpx


def test_production_uvicorn_binds_and_enforces_internal_auth(tmp_path) -> None:
    with socket.socket() as available:
        available.bind(("127.0.0.1", 0))
        port = available.getsockname()[1]
    environment = {
        **os.environ,
        "EQUILIBRAFIT_AI_ENVIRONMENT": "Production",
        "EQUILIBRAFIT_AI_API_KEY": "test-process-key",
        "EQUILIBRAFIT_AI_OPENAI_API_KEY": "",
        "EQUILIBRAFIT_AI_GEMINI_API_KEY": "",
        "EQUILIBRAFIT_AI_ENABLE_DOCS": "false",
        "PYTHONUNBUFFERED": "1",
    }
    log_path = tmp_path / "uvicorn.log"
    with log_path.open("w+", encoding="utf-8") as output:
        process = subprocess.Popen(
            [sys.executable, "-m", "uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", str(port)],
            cwd=Path(__file__).resolve().parents[1],
            env=environment,
            stdout=output,
            stderr=subprocess.STDOUT,
        )
        try:
            with httpx.Client(base_url=f"http://127.0.0.1:{port}", timeout=1, trust_env=False) as client:
                limit = monotonic() + 20
                while True:
                    assert process.poll() is None, "The AI process exited during startup."
                    try:
                        health = client.get("/health")
                        if health.status_code == 200:
                            break
                    except httpx.TransportError:
                        pass
                    assert monotonic() < limit, "The AI process did not become responsive."
                    sleep(0.1)
                assert health.json()["service"] == "equilibrafit-plusplus-ai"
                assert "X-Correlation-ID" in health.headers
                assert client.get("/docs").status_code == 404
                for path in ("/api/v1/coach/chat", "/api/v1/meals/recognize"):
                    assert client.post(path, json={}).status_code == 401
                assert client.get("/health").status_code == 200
        finally:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)
        output.seek(0)
        logs = output.read()
        assert "service.started" in logs
        assert "request.completed" in logs
        assert "test-process-key" not in logs

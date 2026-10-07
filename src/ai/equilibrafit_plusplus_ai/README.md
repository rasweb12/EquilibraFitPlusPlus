# EquilibraFit++ AI

Serviço interno FastAPI para Coach IA, planos, treinos, reconhecimento de refeições e rótulos.

## Execução local

```powershell
cd D:\Curso\C#\EQUILIBRAFIT\src\ai\equilibrafit_plusplus_ai
python -m venv .venv
.\.venv\Scripts\activate
pip install -r requirements-dev.txt
$env:EQUILIBRAFIT_AI_API_KEY="LOCAL_DEVELOPMENT_AI_KEY_CHANGE_IN_KEY_VAULT"
uvicorn app.main:app --host 127.0.0.1 --port 8001
```

Também é possível usar o script da raiz:

```powershell
.\scripts\start-ai.ps1
```

OpenAI, OpenAI Vision e YOLO são opcionais por ambiente:

- `EQUILIBRAFIT_AI_OPENAI_API_KEY`: habilita respostas por OpenAI e análise de imagens por visão.
- `EQUILIBRAFIT_AI_OPENAI_MODEL`: modelo textual.
- `EQUILIBRAFIT_AI_YOLO_MODEL_PATH`: caminho do modelo YOLO local.

Para habilitar OpenCV/YOLO no container de visão:

```powershell
pip install -r requirements-vision.txt
```

Sem essas variáveis, o serviço continua funcional com motor híbrido seguro e revisão manual quando necessário.

## Endpoints

- `GET /health`
- `POST /v1/coach/chat`
- `POST /api/v1/coach/chat`
- `POST /api/v1/coach/message`
- `POST /api/v1/plans/generate`
- `POST /api/v1/workouts/generate`
- `POST /api/v1/meals/recognize`
- `POST /api/v1/meals/estimate-text`
- `POST /api/v1/labels/recognize`

Todos os endpoints internos, exceto `/health`, exigem `X-API-Key` quando `EQUILIBRAFIT_AI_API_KEY` estiver configurada.

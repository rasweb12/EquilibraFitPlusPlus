from pydantic import BaseModel, Field


class CoachMessageRequest(BaseModel):
    """Request sent by the backend to the AI Coach."""

    tenant_id: str | None = None
    usuario_id: str | None = None
    sessao_id: str | None = None
    mensagem: str = Field(min_length=1, max_length=2000)
    system_prompt_version: str = "coach-v1"
    contexto_json: str | None = None
    model: str | None = None


class CoachMessageResponse(BaseModel):
    """Coach response compatible with the ASP.NET Core backend."""

    conteudo: str
    modelo: str
    safety_level: str = "safe"
    fallback_used: bool = False

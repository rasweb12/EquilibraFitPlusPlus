from __future__ import annotations

from datetime import date
from decimal import Decimal

from pydantic import BaseModel, ConfigDict, Field

from app.schemas.common import SafetyNotice


def to_camel(value: str) -> str:
    """Convert snake_case field names to lower camelCase aliases."""
    parts = value.split("_")
    return parts[0] + "".join(part.capitalize() for part in parts[1:])


class ContextModel(BaseModel):
    """Base model for .NET camelCase AI context payloads."""

    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="ignore")


class AiProfileContext(ContextModel):
    """Minimized user profile."""

    idade: int | None = None
    sexo: str | None = None
    altura_cm: Decimal | None = None
    peso_atual_kg: Decimal | None = None
    percentual_gordura: Decimal | None = None
    percentual_massa_magra: Decimal | None = None
    nivel: str | None = None


class AiGoalsContext(ContextModel):
    """User goals relevant to workout decisions."""

    objetivo_principal: str | None = None
    objetivos_secundarios: list[str] = Field(default_factory=list)
    grupos_musculares_prioritarios: list[str] = Field(default_factory=list)
    frequencia_preferida_dias_semana: int | None = Field(default=None, ge=1, le=7)
    duracao_preferida_minutos: int | None = Field(default=None, ge=10, le=240)


class AiRoutineContext(ContextModel):
    """User routine and workout constraints."""

    dias_por_semana: int | None = Field(default=None, ge=1, le=7)
    minutos_disponiveis: int | None = Field(default=None, ge=10, le=240)
    equipamentos: list[str] = Field(default_factory=list)
    ambiente_treino: str | None = None
    limitacoes: list[str] = Field(default_factory=list)
    preferencias_relevantes: list[str] = Field(default_factory=list)


class AiWorkoutDayShapeContext(ContextModel):
    """Current plan day shape."""

    dia_treino: int
    quantidade_exercicios: int


class AiPrescribedExerciseContext(ContextModel):
    """Prescribed exercise from the current plan."""

    exercicio_id: str | None = None
    nome: str
    grupo_muscular: str
    dia_treino: int
    ordem: int
    series: int
    repeticoes: str
    descanso_segundos: int
    carga_alvo_kg: Decimal | None = None
    rpe_alvo: int | None = None
    repeticoes_min: int | None = None
    repeticoes_max: int | None = None
    observacao: str | None = None
    progressao_atual: str | None = None


class AiCurrentWorkoutPlanContext(ContextModel):
    """Current workout plan context."""

    id: str
    versao: int
    fase: str
    semana_atual: int
    duracao_semanas: int
    dias_por_semana: int | None = Field(default=None, ge=1, le=7)
    exercicios_por_dia: list[AiWorkoutDayShapeContext] = Field(default_factory=list)
    exercicios_prescritos: list[AiPrescribedExerciseContext] = Field(default_factory=list)


class AiWorkoutSessionContext(ContextModel):
    """Recent workout session summary."""

    data: date
    dia_treino: int
    duracao_minutos: int | None = None
    teve_dor_desconforto: bool = False
    rpe_maximo: int | None = None
    volume_kg: Decimal = Decimal(0)


class AiPerformedExerciseContext(ContextModel):
    """Performed exercise set from recent history."""

    data: date
    nome: str
    carga_kg: Decimal | None = None
    repeticoes: int
    rpe: int
    volume_kg: Decimal | None = None
    dor_desconforto: bool = False


class AiExerciseProgressionContext(ContextModel):
    """Exercise-level progression summary."""

    nome: str
    carga_inicial_kg: Decimal | None = None
    carga_recente_kg: Decimal | None = None
    repeticoes_recentes: int | None = None
    rpe_recente: int | None = None
    tendencia: str


class AiWorkoutHistoryContext(ContextModel):
    """Recent workout history."""

    janela_dias: int = 30
    sessoes: int = 0
    sessoes_previstas: int | None = None
    aderencia: Decimal | None = None
    aderencia_percentual: Decimal | None = None
    rpe_medio: Decimal | None = None
    rpe_maximo: int | None = None
    volume_total_kg: Decimal = Decimal(0)
    sessoes_recentes: list[AiWorkoutSessionContext] = Field(default_factory=list)
    exercicios_realizados: list[AiPerformedExerciseContext] = Field(default_factory=list)
    progressao: list[AiExerciseProgressionContext] = Field(default_factory=list)
    melhores_cargas: list[AiExerciseLoadSummaryContext] = Field(default_factory=list)
    ultimas_cargas: list[AiExerciseLoadSummaryContext] = Field(default_factory=list)
    exercicios_frequentemente_nao_concluidos: list[str] = Field(default_factory=list)


class AiExerciseLoadSummaryContext(ContextModel):
    """Exercise load summary."""

    nome: str
    carga_kg: Decimal | None = None
    data: date | None = None


class AiWorkoutSafetyContext(ContextModel):
    """Safety signals for workout decisions."""

    dor_desconforto_recente: bool = False
    descricoes_dor_recentes: list[str] = Field(default_factory=list)
    limitacoes_atuais: list[str] = Field(default_factory=list)


class AiBodyEvolutionContext(ContextModel):
    """Body evolution context."""

    peso_atual_kg: Decimal | None = None
    tendencia_peso: str | None = None
    percentual_gordura: Decimal | None = None
    percentual_massa_magra: Decimal | None = None
    ultimo_registro_em: date | None = None
    medidas_relevantes: list[AiBodyMeasurementContext] = Field(default_factory=list)


class AiBodyMeasurementContext(ContextModel):
    """Recent body measurement."""

    nome: str
    valor_cm: Decimal
    data: date


class AiRecoveryContext(ContextModel):
    """Recovery and habit context."""

    sono_recente_horas: Decimal | None = None
    sono_medio_horas: Decimal | None = None
    agua_recente_ml: int | None = None
    agua_media_ml: int | None = None
    aderencia_sono: Decimal | None = None
    aderencia_hidratacao: Decimal | None = None
    humor_medio: Decimal | None = None
    alongamento_recente: bool | None = None
    meditacao_recente: bool | None = None
    habitos_relevantes: list[str] = Field(default_factory=list)


class AiCoachMemoryContext(ContextModel):
    """Confirmed coach memory relevant to personalization."""

    categoria: str
    chave: str
    valor: str
    tipo_fato: str
    confirmado_pelo_usuario: bool


class WorkoutAiContext(ContextModel):
    """Structured workout AI context."""

    context_version: int = 1
    perfil: AiProfileContext | None = None
    objetivos: AiGoalsContext | None = None
    rotina: AiRoutineContext | None = None
    plano_atual: AiCurrentWorkoutPlanContext | None = None
    historico_recente: AiWorkoutHistoryContext | None = None
    seguranca: AiWorkoutSafetyContext | None = None
    evolucao: AiBodyEvolutionContext | None = None
    recuperacao: AiRecoveryContext | None = None
    preferencias_relevantes: list[AiCoachMemoryContext] = Field(default_factory=list)


class WorkoutGenerateRequest(BaseModel):
    """Workout generation request."""

    objective: str
    level: str = "iniciante"
    days_per_week: int = Field(default=3, ge=1, le=7)
    duration_minutes: int | None = Field(default=None, ge=10, le=240)
    limitations: list[str] = Field(default_factory=list)
    equipment: list[str] = Field(default_factory=list)
    priority_muscle_groups: list[str] = Field(default_factory=list)
    operational_guidance: str | None = Field(default=None, max_length=12000)
    workout_ai_context: WorkoutAiContext | None = None
    prompt_version: str = Field(default="workout-context-v1", max_length=80)


class WorkoutDay(BaseModel):
    """One workout day."""

    name: str
    focus: str
    exercises: list[str]


class RecommendationRationale(BaseModel):
    """Structured rationale for an AI recommendation."""

    recommendation: str
    reason: str
    confidence: float = Field(ge=0, le=1)


class WorkoutGenerateResponse(BaseModel):
    """Workout generation response."""

    frequency: int
    days: list[WorkoutDay]
    progression: str
    safety_notices: list[SafetyNotice]
    model: str
    fallback_used: bool
    rationale: RecommendationRationale
    retrieval_used: bool = False
    retrieved_document_ids: list[str] = Field(default_factory=list)

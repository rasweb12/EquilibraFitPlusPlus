from __future__ import annotations

import re
import unicodedata
from dataclasses import dataclass, field
from datetime import UTC, datetime
from math import sqrt
from typing import Protocol


@dataclass(frozen=True)
class KnowledgeDocument:
    """Versioned internal knowledge document."""

    document_id: str
    document_type: str
    title: str
    topic: str
    muscle_group: str | None
    equipment: str | None
    level: str | None
    language: str
    source: str
    version: str
    updated_at: datetime
    content: str


@dataclass(frozen=True)
class KnowledgeChunk:
    """Chunk selected for prompt grounding."""

    chunk_id: str
    document_id: str
    title: str
    section: str
    content: str
    metadata: dict[str, str | None]
    score: float = 0


@dataclass(frozen=True)
class RetrievalResult:
    """Hybrid retrieval result with selected chunks."""

    chunks: list[KnowledgeChunk] = field(default_factory=list)

    @property
    def document_ids(self) -> list[str]:
        """Return stable document ids without duplicates."""
        return list(dict.fromkeys(chunk.document_id for chunk in self.chunks))


class IEmbeddingService(Protocol):
    """Embedding abstraction prepared for provider-specific implementations."""

    def embed(self, text: str) -> list[float]:
        """Return a vector representation for text."""


class IVectorKnowledgeStore(Protocol):
    """Vector store abstraction used by the retrieval pipeline."""

    def hybrid_search(
        self,
        query: str,
        *,
        metadata_filters: dict[str, str | None] | None = None,
        limit: int = 4,
    ) -> RetrievalResult:
        """Search by vector, keyword and metadata filters."""


class HashingEmbeddingService:
    """Small deterministic embedding used when no external vector provider exists."""

    dimensions = 64

    def embed(self, text: str) -> list[float]:
        vector = [0.0] * self.dimensions
        for token in _tokens(text):
            vector[hash(token) % self.dimensions] += 1.0
        norm = sqrt(sum(value * value for value in vector))
        return [value / norm for value in vector] if norm else vector


class InMemoryHybridKnowledgeStore:
    """In-memory hybrid store for approved EquilibraFit knowledge."""

    def __init__(self, documents: list[KnowledgeDocument] | None = None, embedding_service: IEmbeddingService | None = None) -> None:
        self._embedding_service = embedding_service or HashingEmbeddingService()
        self._chunks = [
            chunk
            for document in documents or APPROVED_DOCUMENTS
            for chunk in chunk_document(document)
        ]
        self._vectors = {chunk.chunk_id: self._embedding_service.embed(chunk.content) for chunk in self._chunks}

    def hybrid_search(
        self,
        query: str,
        *,
        metadata_filters: dict[str, str | None] | None = None,
        limit: int = 4,
    ) -> RetrievalResult:
        normalized_query = _normalize(query)
        query_vector = self._embedding_service.embed(query)
        scored: list[KnowledgeChunk] = []

        for chunk in self._chunks:
            if not _metadata_matches(chunk.metadata, metadata_filters or {}):
                continue

            keyword_score = _keyword_score(normalized_query, _normalize(chunk.content))
            vector_score = _cosine(query_vector, self._vectors[chunk.chunk_id])
            metadata_score = _metadata_score(chunk.metadata, metadata_filters or {})
            score = (0.55 * keyword_score) + (0.35 * vector_score) + (0.10 * metadata_score)
            if score <= 0:
                continue

            scored.append(
                KnowledgeChunk(
                    chunk.chunk_id,
                    chunk.document_id,
                    chunk.title,
                    chunk.section,
                    chunk.content,
                    chunk.metadata,
                    score,
                )
            )

        return RetrievalResult(sorted(scored, key=lambda item: item.score, reverse=True)[: max(limit, 0)])


APPROVED_DOCUMENTS = [
    KnowledgeDocument(
        document_id="eqfit-workout-progression-v1",
        document_type="training_guideline",
        title="Progressão de carga e repetições",
        topic="progression",
        muscle_group=None,
        equipment=None,
        level=None,
        language="pt-BR",
        source="EquilibraFit interno",
        version="1.0",
        updated_at=datetime(2026, 8, 13, tzinfo=UTC),
        content=(
            "Critério de progressão\n"
            "Aumentar carga exige histórico real suficiente, execução estável, ausência de dor e RPE dentro da faixa planejada. "
            "Para faixas como 8-12 repetições, manter a carga é adequado quando o usuário ainda não atingiu o topo da faixa em todas as séries.\n\n"
            "RPE e segurança\n"
            "RPE 9 ou 10 indica esforço muito alto. Nesses casos, priorize manutenção de carga, redução de volume ou descanso adicional."
        ),
    ),
    KnowledgeDocument(
        document_id="eqfit-workout-pain-safety-v1",
        document_type="safety_policy",
        title="Dor, desconforto e sinais de alerta",
        topic="safety",
        muscle_group=None,
        equipment=None,
        level=None,
        language="pt-BR",
        source="EquilibraFit interno",
        version="1.0",
        updated_at=datetime(2026, 8, 13, tzinfo=UTC),
        content=(
            "Dor recente\n"
            "Quando houver dor ou desconforto recente, não aumente carga automaticamente. Ajuste amplitude, reduza intensidade ou proponha revisão profissional.\n\n"
            "Sinais de alerta\n"
            "Dor no peito, falta de ar incomum, desmaio, lesão importante, gestação ou condição médica reduzem a autonomia da IA e exigem orientação profissional."
        ),
    ),
    KnowledgeDocument(
        document_id="eqfit-machine-chest-v1",
        document_type="exercise_guideline",
        title="Supino máquina e chest press",
        topic="exercise_execution",
        muscle_group="peito",
        equipment="máquina",
        level="iniciante",
        language="pt-BR",
        source="EquilibraFit interno",
        version="1.0",
        updated_at=datetime(2026, 8, 13, tzinfo=UTC),
        content=(
            "Execução\n"
            "No supino máquina ou chest press, ajuste o banco para alinhar as mãos ao meio do peitoral, mantenha escápulas estáveis e controle a fase excêntrica.\n\n"
            "Alternativas\n"
            "Quando halteres não estiverem disponíveis, chest press máquina pode substituir supino com halteres mantendo padrão de empurrar horizontal."
        ),
    ),
    KnowledgeDocument(
        document_id="eqfit-recovery-hydration-v1",
        document_type="recovery_guideline",
        title="Sono, hidratação e recuperação",
        topic="recovery",
        muscle_group=None,
        equipment=None,
        level=None,
        language="pt-BR",
        source="EquilibraFit interno",
        version="1.0",
        updated_at=datetime(2026, 8, 13, tzinfo=UTC),
        content=(
            "Recuperação\n"
            "Sono abaixo da meta, baixa hidratação e humor baixo são sinais para evitar progressão agressiva. A recomendação deve favorecer consistência e técnica.\n\n"
            "Treino curto\n"
            "Quando o tempo disponível for limitado, reduza acessórios primeiro e preserve exercícios principais seguros e já tolerados pelo usuário."
        ),
    ),
]


def retrieve_guidance(query: str) -> list[str]:
    """Return approved guidance snippets for deterministic coach fallback."""
    result = _DEFAULT_STORE.hybrid_search(query, limit=3)
    return [chunk.content for chunk in result.chunks] or [APPROVED_DOCUMENTS[0].content]


def retrieve_workout_knowledge(
    query: str,
    *,
    equipment: list[str] | None = None,
    muscle_groups: list[str] | None = None,
    level: str | None = None,
    limit: int = 4,
) -> RetrievalResult:
    """Retrieve grounded workout knowledge with lightweight metadata filters."""
    filters = {
        "equipment": _first(equipment),
        "muscle_group": _first(muscle_groups),
        "level": level,
        "language": "pt-BR",
    }
    return _DEFAULT_STORE.hybrid_search(query, metadata_filters=filters, limit=limit)


def chunk_document(document: KnowledgeDocument) -> list[KnowledgeChunk]:
    """Split a document into section-preserving chunks."""
    sections = re.split(r"\n\s*\n", document.content.strip())
    chunks: list[KnowledgeChunk] = []
    for index, section_text in enumerate(sections, start=1):
        lines = [line.strip() for line in section_text.splitlines() if line.strip()]
        if not lines:
            continue
        section = lines[0]
        content = f"{document.title} / {section}: {' '.join(lines[1:] or lines)}"
        chunks.append(
            KnowledgeChunk(
                chunk_id=f"{document.document_id}#chunk-{index}",
                document_id=document.document_id,
                title=document.title,
                section=section,
                content=content,
                metadata={
                    "document_type": document.document_type,
                    "topic": document.topic,
                    "muscle_group": document.muscle_group,
                    "equipment": document.equipment,
                    "level": document.level,
                    "language": document.language,
                    "source": document.source,
                    "version": document.version,
                    "updated_at": document.updated_at.date().isoformat(),
                },
            )
        )
    return chunks


def _metadata_matches(metadata: dict[str, str | None], filters: dict[str, str | None]) -> bool:
    for key, expected in filters.items():
        if not expected:
            continue
        actual = metadata.get(key)
        if actual and _normalize(expected) not in _normalize(actual):
            return False
    return True


def _metadata_score(metadata: dict[str, str | None], filters: dict[str, str | None]) -> float:
    active = [(key, value) for key, value in filters.items() if value]
    if not active:
        return 0.5
    matches = sum(1 for key, value in active if metadata.get(key) and _normalize(value or "") in _normalize(metadata[key] or ""))
    return matches / len(active)


def _keyword_score(query: str, content: str) -> float:
    query_terms = set(_tokens(query))
    if not query_terms:
        return 0
    content_terms = set(_tokens(content))
    return len(query_terms & content_terms) / len(query_terms)


def _cosine(left: list[float], right: list[float]) -> float:
    return sum(a * b for a, b in zip(left, right, strict=False))


def _tokens(value: str) -> list[str]:
    return [token for token in re.split(r"[^a-z0-9]+", _normalize(value)) if len(token) > 2]


def _normalize(value: str) -> str:
    decomposed = unicodedata.normalize("NFD", value.lower())
    return "".join(char for char in decomposed if unicodedata.category(char) != "Mn")


def _first(values: list[str] | None) -> str | None:
    return next((value for value in values or [] if value), None)


_DEFAULT_STORE = InMemoryHybridKnowledgeStore()

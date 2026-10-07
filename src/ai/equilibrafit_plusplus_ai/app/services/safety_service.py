import unicodedata

PUNITIVE_TERMS = (
    "você falhou",
    "saiu da dieta",
    "estragou tudo",
    "compense imediatamente",
    "proibido",
    "nunca mais coma",
)

CLINICAL_RISK_TERMS = (
    "diagnóstico",
    "prescrição",
    "remédio",
    "dose",
    "tratamento para",
    "pare de tomar",
)


class SafetyResult:
    """Safety validation result."""

    def __init__(self, is_safe: bool, reason: str | None = None) -> None:
        self.is_safe = is_safe
        self.reason = reason


class SafetyService:
    """Validates generated text against product and health safety rules."""

    def validate_text(self, text: str) -> SafetyResult:
        """Validate text content."""
        normalized = _normalize(text)
        for term in PUNITIVE_TERMS:
            if _normalize(term) in normalized:
                return SafetyResult(False, "linguagem_punitiva")

        for term in CLINICAL_RISK_TERMS:
            if _normalize(term) in normalized:
                return SafetyResult(False, "risco_clinico")

        return SafetyResult(True)

    def safe_fallback(self, topic: str = "coach") -> str:
        """Return a safe fallback message."""
        if topic == "meal":
            return "Não conseguimos estimar essa refeição com segurança agora. Você pode registrar manualmente e seguir normalmente."

        if topic == "plan":
            return "Podemos montar uma proposta simples e ajustar depois conforme sua rotina e preferências."

        return (
            "Sem problemas. Podemos ajustar com calma e continuar pelo caminho mais simples agora. "
            "O Coach IA orienta e educa, mas não substitui profissionais habilitados."
        )


def _normalize(value: str) -> str:
    decomposed = unicodedata.normalize("NFD", value.lower())
    return "".join(char for char in decomposed if unicodedata.category(char) != "Mn")

from app.services.safety_service import SafetyService


def test_safety_service_blocks_punitive_language() -> None:
    service = SafetyService()

    result = service.validate_text("Você falhou e saiu da dieta.")

    assert not result.is_safe
    assert result.reason == "linguagem_punitiva"


def test_safety_service_accepts_supportive_language() -> None:
    service = SafetyService()

    result = service.validate_text("Sem problemas. Podemos ajustar e continuar.")

    assert result.is_safe

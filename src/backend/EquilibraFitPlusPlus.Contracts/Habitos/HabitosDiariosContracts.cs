namespace EquilibraFitPlusPlus.Contracts.Habitos;

/// <summary>
/// Request used to save or update daily habits.
/// </summary>
public sealed record SalvarHabitosDiariosRequest(
    DateOnly? Data,
    int AguaMl,
    int MetaAguaMl,
    decimal SonoHoras,
    decimal MetaSonoHoras,
    int Humor,
    bool MeditacaoRealizada,
    bool AlongamentoRealizado);

/// <summary>
/// Daily habits response.
/// </summary>
public sealed record HabitosDiariosResponse(
    Guid? Id,
    DateOnly Data,
    int AguaMl,
    int MetaAguaMl,
    decimal SonoHoras,
    decimal MetaSonoHoras,
    int Humor,
    bool MeditacaoRealizada,
    bool AlongamentoRealizado,
    string Mensagem);

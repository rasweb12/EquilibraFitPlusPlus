namespace EquilibraFitPlusPlus.Contracts.Admin;

/// <summary>
/// Administrative dashboard response.
/// </summary>
public sealed record AdminDashboardResponse(
    int UsuariosTotal,
    int UsuariosAtivos,
    int PerfisPreenchidos,
    int RefeicoesHoje,
    int TreinosAtivos,
    int SessoesCoachHoje,
    int MensagensCoachHoje,
    int AssinaturasAtivas,
    decimal ReceitaMes,
    IReadOnlyCollection<string> Alertas);

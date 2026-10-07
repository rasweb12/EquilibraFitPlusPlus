namespace EquilibraFitPlusPlus.Application.Abstractions.AiCoach;

/// <summary>
/// Provides approved operational instructions used by AI workflows.
/// </summary>
public interface IAiInstructionRepository
{
    /// <summary>
    /// Gets the currently published AI teaching configuration.
    /// </summary>
    Task<AiInstructionSet?> ObterInstrucoesPublicadasAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>
/// Approved AI instructions used by runtime AI requests.
/// </summary>
public sealed record AiInstructionSet(
    string Versao,
    string InstrucoesCoach,
    string RegrasAlimentacao,
    string RegrasTreino,
    string BaseConhecimento,
    string ExemplosBoasRespostas)
{
    /// <summary>
    /// Converts the instruction set to a compact prompt fragment.
    /// </summary>
    public string ToPromptFragment()
    {
        return string.Join(
            "\n",
            [
                $"Versao publicada: {Versao}",
                $"Instrucoes Coach IA: {InstrucoesCoach}",
                $"Regras de alimentação: {RegrasAlimentacao}",
                $"Regras de treino: {RegrasTreino}",
                $"Base aprovada: {BaseConhecimento}",
                $"Exemplos aprovados: {ExemplosBoasRespostas}"
            ]);
    }
}

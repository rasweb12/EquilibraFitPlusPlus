using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for AI operation administration.
/// </summary>
public sealed class AdminIaRepository : IAdminIaRepository
{
    private const string CurrentMetadataKey = "ia.operacao.ensino.current.metadata";
    private const string CurrentCoachKey = "ia.operacao.ensino.current.coach";
    private const string CurrentFoodKey = "ia.operacao.ensino.current.food";
    private const string CurrentWorkoutKey = "ia.operacao.ensino.current.workout";
    private const string CurrentKnowledgeKey = "ia.operacao.ensino.current.knowledge";
    private const string CurrentExamplesKey = "ia.operacao.ensino.current.examples";
    private const string VersionKeyPrefix = "ia.operacao.ensino.version.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex EmailRegex = new(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DocumentRegex = new(@"\b\d{4,}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public AdminIaRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<AdminIaEnsinoSourceData> ObterEnsinoAtualAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        TeachingMetadata? metadata = await ObterMetadataAsync(tenantId, CurrentMetadataKey, cancellationToken);
        AdminIaEnsinoSourceData defaults = CreateDefaultTeaching();

        if (metadata is null)
        {
            return defaults;
        }

        return new AdminIaEnsinoSourceData(
            metadata.Versao,
            await ObterValorOuPadraoAsync(tenantId, CurrentCoachKey, defaults.InstrucoesCoach, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, CurrentFoodKey, defaults.RegrasAlimentacao, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, CurrentWorkoutKey, defaults.RegrasTreino, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, CurrentKnowledgeKey, defaults.BaseConhecimento, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, CurrentExamplesKey, defaults.ExemplosBoasRespostas, cancellationToken),
            true,
            metadata.PublicadaEm,
            metadata.Observacao);
    }

    /// <inheritdoc />
    public async Task<AiInstructionSet?> ObterInstrucoesPublicadasAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        AdminIaEnsinoSourceData ensino = await ObterEnsinoAtualAsync(tenantId, cancellationToken);
        return new AiInstructionSet(
            ensino.Versao,
            ensino.InstrucoesCoach,
            ensino.RegrasAlimentacao,
            ensino.RegrasTreino,
            ensino.BaseConhecimento,
            ensino.ExemplosBoasRespostas);
    }

    /// <inheritdoc />
    public async Task<AdminIaMetricasSourceData> ObterMetricasAsync(Guid tenantId, DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        DateTimeOffset dayStart = ToUtcStart(dataReferencia);
        DateTimeOffset dayEndExclusive = ToUtcStart(dataReferencia.AddDays(1));
        DateTimeOffset thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

        int sessoesCoachHoje = await _dbContext.ChatSessions.CountAsync(session =>
            session.TenantId == tenantId && session.CriadoEm >= dayStart && session.CriadoEm < dayEndExclusive, cancellationToken);
        int mensagensCoachHoje = await _dbContext.ChatMessages.CountAsync(message =>
            message.TenantId == tenantId && message.CriadoEm >= dayStart && message.CriadoEm < dayEndExclusive, cancellationToken);
        int reconhecimentosImagemHoje = await _dbContext.AnalisesRefeicaoImagem.CountAsync(analise =>
            analise.TenantId == tenantId && analise.CriadoEm >= dayStart && analise.CriadoEm < dayEndExclusive, cancellationToken);
        int planosGeradosHoje = await _dbContext.PlanosUsuario.CountAsync(plano =>
            plano.TenantId == tenantId && plano.CriadoEm >= dayStart && plano.CriadoEm < dayEndExclusive, cancellationToken);
        int treinosGeradosHoje = await _dbContext.TreinosUsuario.CountAsync(treino =>
            treino.TenantId == tenantId && treino.CriadoEm >= dayStart && treino.CriadoEm < dayEndExclusive, cancellationToken);
        int coachFallbacksHoje = await _dbContext.ChatMessages.CountAsync(message =>
            message.TenantId == tenantId
            && message.Role == "assistant"
            && message.ModeloIa == "equilibrafit-coach-rules-v1"
            && message.CriadoEm >= dayStart
            && message.CriadoEm < dayEndExclusive,
            cancellationToken);
        int planosFallbackHoje = await _dbContext.PlanosUsuario.CountAsync(plano =>
            plano.TenantId == tenantId
            && plano.FonteGeracao == "Hibrido"
            && plano.CriadoEm >= dayStart
            && plano.CriadoEm < dayEndExclusive,
            cancellationToken);
        int respostasBloqueadas30Dias = await _dbContext.Auditorias.CountAsync(auditoria =>
            auditoria.TenantId == tenantId
            && auditoria.Acao == "ia.resposta.bloqueada"
            && auditoria.CriadoEm >= thirtyDaysAgo,
            cancellationToken);

        return new AdminIaMetricasSourceData(
            sessoesCoachHoje,
            mensagensCoachHoje,
            reconhecimentosImagemHoje,
            planosGeradosHoje,
            treinosGeradosHoje,
            coachFallbacksHoje + planosFallbackHoje,
            respostasBloqueadas30Dias);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AdminIaHistoricoSourceData>> ListarHistoricoAsync(Guid tenantId, int quantidade, CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(quantidade, 1, 100);

        AdminIaHistoricoSourceData[] mensagens = await _dbContext.ChatMessages
            .Where(message => message.TenantId == tenantId && message.Role == "assistant")
            .OrderByDescending(message => message.CriadoEm)
            .Take(limit)
            .Select(message => new AdminIaHistoricoSourceData(
                message.Id,
                "Coach IA",
                message.Conteudo,
                "Respondida",
                message.ModeloIa,
                message.CriadoEm))
            .ToArrayAsync(cancellationToken);

        var imagensRaw = await _dbContext.AnalisesRefeicaoImagem
            .Where(analise => analise.TenantId == tenantId)
            .OrderByDescending(analise => analise.CriadoEm)
            .Take(limit)
            .Select(analise => new
            {
                analise.Id,
                analise.ConfiancaMedia,
                analise.Status,
                analise.ModeloVisao,
                analise.CriadoEm
            })
            .ToArrayAsync(cancellationToken);

        AdminIaHistoricoSourceData[] imagens = imagensRaw
            .Select(analise => new AdminIaHistoricoSourceData(
                analise.Id,
                "Reconhecimento de refeição",
                "Imagem analisada com " + analise.ConfiancaMedia.ToString("0.##", CultureInfo.InvariantCulture) + "% de confiança média.",
                analise.Status.ToString(),
                analise.ModeloVisao,
                analise.CriadoEm))
            .ToArray();

        var planosRaw = await _dbContext.PlanosUsuario
            .Where(plano => plano.TenantId == tenantId)
            .OrderByDescending(plano => plano.CriadoEm)
            .Take(limit)
            .Select(plano => new
            {
                plano.Id,
                plano.FonteGeracao,
                plano.CaloriasDia,
                plano.Status,
                plano.ModeloIaVersao,
                plano.CriadoEm
            })
            .ToArrayAsync(cancellationToken);

        AdminIaHistoricoSourceData[] planos = planosRaw
            .Select(plano => new AdminIaHistoricoSourceData(
                plano.Id,
                "Plano alimentar",
                "Plano " + plano.FonteGeracao + " com " + plano.CaloriasDia.ToString(CultureInfo.InvariantCulture) + " kcal/dia.",
                plano.Status.ToString(),
                plano.ModeloIaVersao,
                plano.CriadoEm))
            .ToArray();

        var treinosRaw = await _dbContext.TreinosUsuario
            .Where(treino => treino.TenantId == tenantId)
            .OrderByDescending(treino => treino.CriadoEm)
            .Take(limit)
            .Select(treino => new
            {
                treino.Id,
                treino.Nome,
                treino.FrequenciaSemanal,
                treino.Ativo,
                treino.CriadoEm
            })
            .ToArrayAsync(cancellationToken);

        AdminIaHistoricoSourceData[] treinos = treinosRaw
            .Select(treino => new AdminIaHistoricoSourceData(
                treino.Id,
                "Treino",
                treino.Nome + " com " + treino.FrequenciaSemanal.ToString(CultureInfo.InvariantCulture) + " dias por semana.",
                treino.Ativo ? "Ativo" : "Histórico",
                null,
                treino.CriadoEm))
            .ToArray();

        AdminIaHistoricoSourceData[] bloqueios = await _dbContext.Auditorias
            .Where(auditoria => auditoria.TenantId == tenantId && auditoria.Acao == "ia.resposta.bloqueada")
            .OrderByDescending(auditoria => auditoria.CriadoEm)
            .Take(limit)
            .Select(auditoria => new AdminIaHistoricoSourceData(
                auditoria.Id,
                "Resposta bloqueada",
                auditoria.MetadadosJson ?? "Resposta bloqueada pela validação de segurança.",
                "Bloqueada",
                null,
                auditoria.CriadoEm))
            .ToArrayAsync(cancellationToken);

        return mensagens
            .Concat(imagens)
            .Concat(planos)
            .Concat(treinos)
            .Concat(bloqueios)
            .OrderByDescending(item => item.CriadoEm)
            .Take(limit)
            .Select(item => item with { Resumo = Sanitize(item.Resumo) })
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<AdminIaEnsinoSourceData?> ObterEnsinoPorVersaoAsync(Guid tenantId, string versao, CancellationToken cancellationToken)
    {
        string version = NormalizeVersion(versao);
        string prefix = BuildVersionPrefix(version);
        TeachingMetadata? metadata = await ObterMetadataAsync(tenantId, prefix + "metadata", cancellationToken);
        if (metadata is null)
        {
            return null;
        }

        AdminIaEnsinoSourceData defaults = CreateDefaultTeaching();
        return new AdminIaEnsinoSourceData(
            metadata.Versao,
            await ObterValorOuPadraoAsync(tenantId, prefix + "coach", defaults.InstrucoesCoach, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, prefix + "food", defaults.RegrasAlimentacao, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, prefix + "workout", defaults.RegrasTreino, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, prefix + "knowledge", defaults.BaseConhecimento, cancellationToken),
            await ObterValorOuPadraoAsync(tenantId, prefix + "examples", defaults.ExemplosBoasRespostas, cancellationToken),
            true,
            metadata.PublicadaEm,
            metadata.Observacao);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AdminIaEnsinoVersaoSourceData>> ListarVersoesEnsinoAsync(Guid tenantId, int quantidade, CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(quantidade, 1, 100);
        string[] metadataValues = await _dbContext.ConfiguracoesSistema
            .Where(config => config.TenantId == tenantId
                && config.Chave.StartsWith(VersionKeyPrefix)
                && config.Chave.EndsWith(".metadata"))
            .OrderByDescending(config => config.CriadoEm)
            .Take(limit)
            .Select(config => config.Valor)
            .ToArrayAsync(cancellationToken);

        return metadataValues
            .Select(ParseMetadata)
            .Where(metadata => metadata is not null)
            .Select(metadata => new AdminIaEnsinoVersaoSourceData(metadata!.Versao, metadata.PublicadaEm, metadata.Observacao))
            .OrderByDescending(version => version.PublicadaEm)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task SalvarEnsinoPublicadoAsync(Guid tenantId, AdminIaEnsinoSourceData ensino, CancellationToken cancellationToken)
    {
        string version = NormalizeVersion(ensino.Versao);
        DateTimeOffset publishedAt = ensino.PublicadaEm ?? DateTimeOffset.UtcNow;
        var metadata = new TeachingMetadata(version, publishedAt, NormalizeOptional(ensino.Observacao));
        string metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        string versionPrefix = BuildVersionPrefix(version);

        await UpsertAsync(tenantId, CurrentMetadataKey, metadataJson, cancellationToken);
        await UpsertAsync(tenantId, CurrentCoachKey, NormalizeText(ensino.InstrucoesCoach), cancellationToken);
        await UpsertAsync(tenantId, CurrentFoodKey, NormalizeText(ensino.RegrasAlimentacao), cancellationToken);
        await UpsertAsync(tenantId, CurrentWorkoutKey, NormalizeText(ensino.RegrasTreino), cancellationToken);
        await UpsertAsync(tenantId, CurrentKnowledgeKey, NormalizeText(ensino.BaseConhecimento), cancellationToken);
        await UpsertAsync(tenantId, CurrentExamplesKey, NormalizeText(ensino.ExemplosBoasRespostas), cancellationToken);

        await UpsertAsync(tenantId, versionPrefix + "metadata", metadataJson, cancellationToken);
        await UpsertAsync(tenantId, versionPrefix + "coach", NormalizeText(ensino.InstrucoesCoach), cancellationToken);
        await UpsertAsync(tenantId, versionPrefix + "food", NormalizeText(ensino.RegrasAlimentacao), cancellationToken);
        await UpsertAsync(tenantId, versionPrefix + "workout", NormalizeText(ensino.RegrasTreino), cancellationToken);
        await UpsertAsync(tenantId, versionPrefix + "knowledge", NormalizeText(ensino.BaseConhecimento), cancellationToken);
        await UpsertAsync(tenantId, versionPrefix + "examples", NormalizeText(ensino.ExemplosBoasRespostas), cancellationToken);
    }

    private static AdminIaEnsinoSourceData CreateDefaultTeaching()
    {
        return new AdminIaEnsinoSourceData(
            "base-v1",
            "Responda com acolhimento, autonomia do usuário e linguagem sem culpa. Nunca use linguagem punitiva ou extremista. Reforce que a IA orienta e não substitui profissionais de saúde.",
            "Priorize alimentação flexível, culturalmente possível, com porções ajustáveis. Evite extremos, promessas clínicas, proibições absolutas e metas agressivas.",
            "Priorize progressão gradual, técnica, recuperação e adaptação a dor, rotina e limitações. Não incentive excesso, dor ou substituição de educador físico, médico ou fisioterapeuta.",
            "EquilibraFit++ trabalha com melhoria contínua de saúde, treino, alimentação e hábitos. O usuário tem liberdade e os planos devem ser ajustáveis.",
            "Boa resposta: Sem problemas. Podemos ajustar o restante do dia com leveza e continuar no seu ritmo.",
            false,
            null,
            "Configuração base do produto.");
    }

    private async Task<TeachingMetadata?> ObterMetadataAsync(Guid tenantId, string key, CancellationToken cancellationToken)
    {
        string? value = await _dbContext.ConfiguracoesSistema
            .Where(config => config.TenantId == tenantId && config.Chave == key)
            .Select(config => config.Valor)
            .FirstOrDefaultAsync(cancellationToken);

        return ParseMetadata(value);
    }

    private async Task<string> ObterValorOuPadraoAsync(Guid tenantId, string key, string defaultValue, CancellationToken cancellationToken)
    {
        string? value = await _dbContext.ConfiguracoesSistema
            .Where(config => config.TenantId == tenantId && config.Chave == key)
            .Select(config => config.Valor)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private async Task UpsertAsync(Guid tenantId, string key, string value, CancellationToken cancellationToken)
    {
        ConfiguracaoSistema? config = await _dbContext.ConfiguracoesSistema
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Chave == key, cancellationToken);

        if (config is null)
        {
            _dbContext.ConfiguracoesSistema.Add(new ConfiguracaoSistema
            {
                TenantId = tenantId,
                Chave = key,
                Valor = Truncate(value, 4000),
                Sensivel = false
            });

            return;
        }

        config.Valor = Truncate(value, 4000);
        config.Sensivel = false;
    }

    private static TeachingMetadata? ParseMetadata(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TeachingMetadata>(value, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildVersionPrefix(string version)
    {
        return VersionKeyPrefix + NormalizeVersion(version) + ".";
    }

    private static string NormalizeText(string value)
    {
        return value.Trim();
    }

    private static string NormalizeVersion(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Sanitize(string value)
    {
        string sanitized = EmailRegex.Replace(value, "[email]");
        sanitized = DocumentRegex.Replace(sanitized, "[numero]");
        return Truncate(sanitized.Replace("\r", " ").Replace("\n", " "), 180);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static DateTimeOffset ToUtcStart(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private sealed record TeachingMetadata(string Versao, DateTimeOffset PublicadaEm, string? Observacao);
}

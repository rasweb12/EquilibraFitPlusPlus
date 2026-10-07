namespace EquilibraFitPlusPlus.Domain.Enums;

/// <summary>
/// User account status.
/// </summary>
public enum UsuarioStatus
{
    /// <summary>Active user.</summary>
    Ativo = 1,

    /// <summary>Temporarily blocked user.</summary>
    Bloqueado = 2,

    /// <summary>Logically deleted user.</summary>
    Excluido = 3
}

/// <summary>
/// Application user roles.
/// </summary>
public enum UsuarioRole
{
    /// <summary>Regular application user.</summary>
    Usuario = 1,

    /// <summary>Platform administrator.</summary>
    Administrador = 2,

    /// <summary>Support operator.</summary>
    Suporte = 3,

    /// <summary>Content manager.</summary>
    Conteudo = 4,

    /// <summary>AI operations manager.</summary>
    OperacaoIa = 5,

    /// <summary>Financial operator.</summary>
    Financeiro = 6
}

/// <summary>
/// Biological sex used only for metabolic calculations.
/// </summary>
public enum SexoBiologico
{
    /// <summary>Female biological sex.</summary>
    Feminino = 1,

    /// <summary>Male biological sex.</summary>
    Masculino = 2,

    /// <summary>Not informed by the user.</summary>
    NaoInformado = 3
}

/// <summary>
/// User health objective.
/// </summary>
public enum ObjetivoSaude
{
    /// <summary>Sustainable weight loss objective.</summary>
    EmagrecimentoSustentavel = 1,

    /// <summary>Muscle gain objective.</summary>
    GanhoMassa = 2,

    /// <summary>Maintenance objective.</summary>
    Manutencao = 3,

    /// <summary>General conditioning objective.</summary>
    Condicionamento = 4
}

/// <summary>
/// User physical activity level.
/// </summary>
public enum NivelAtividade
{
    /// <summary>Sedentary activity level.</summary>
    Sedentario = 1,

    /// <summary>Light activity level.</summary>
    Leve = 2,

    /// <summary>Moderate activity level.</summary>
    Moderado = 3,

    /// <summary>Intense activity level.</summary>
    Intenso = 4,

    /// <summary>Very intense activity level.</summary>
    MuitoIntenso = 5
}

/// <summary>
/// Consent type.
/// </summary>
public enum TipoConsentimento
{
    /// <summary>Terms of use consent.</summary>
    TermosUso = 1,

    /// <summary>Privacy policy consent.</summary>
    Privacidade = 2,

    /// <summary>AI usage consent.</summary>
    UsoIa = 3,

    /// <summary>Analytics consent.</summary>
    Analytics = 4,

    /// <summary>Marketplace consent.</summary>
    Marketplace = 5
}

/// <summary>
/// User plan status.
/// </summary>
public enum StatusPlano
{
    /// <summary>Active plan.</summary>
    Ativo = 1,

    /// <summary>Plan replaced by a newer version.</summary>
    Substituido = 2,

    /// <summary>Cancelled plan.</summary>
    Cancelado = 3
}

/// <summary>
/// Meal type.
/// </summary>
public enum TipoRefeicao
{
    /// <summary>Breakfast.</summary>
    CafeManha = 1,

    /// <summary>Lunch.</summary>
    Almoco = 2,

    /// <summary>Dinner.</summary>
    Jantar = 3,

    /// <summary>Snack.</summary>
    Lanche = 4,

    /// <summary>Late meal.</summary>
    Ceia = 5,

    /// <summary>Other meal type.</summary>
    Outro = 6
}

/// <summary>
/// Meal entry origin.
/// </summary>
public enum OrigemRegistroAlimentar
{
    /// <summary>Manual entry.</summary>
    Manual = 1,

    /// <summary>Photo recognition entry.</summary>
    Foto = 2,

    /// <summary>Predefined meal entry.</summary>
    Predefinida = 3,

    /// <summary>Nutrition label entry.</summary>
    Rotulo = 4
}

/// <summary>
/// AI image analysis status.
/// </summary>
public enum StatusAnaliseImagem
{
    /// <summary>Pending analysis.</summary>
    Pendente = 1,

    /// <summary>Confirmed by the user.</summary>
    Confirmada = 2,

    /// <summary>Discarded by the user.</summary>
    Descartada = 3,

    /// <summary>Analysis failed.</summary>
    Falhou = 4
}

/// <summary>
/// Subscription status.
/// </summary>
public enum StatusAssinatura
{
    /// <summary>Trial subscription.</summary>
    Trial = 1,

    /// <summary>Active subscription.</summary>
    Ativa = 2,

    /// <summary>Overdue subscription.</summary>
    Atrasada = 3,

    /// <summary>Cancelled subscription.</summary>
    Cancelada = 4,

    /// <summary>Expired subscription.</summary>
    Expirada = 5
}

/// <summary>
/// Payment status.
/// </summary>
public enum StatusPagamento
{
    /// <summary>Pending payment.</summary>
    Pendente = 1,

    /// <summary>Paid payment.</summary>
    Pago = 2,

    /// <summary>Failed payment.</summary>
    Falhou = 3,

    /// <summary>Refunded payment.</summary>
    Reembolsado = 4,

    /// <summary>Cancelled payment.</summary>
    Cancelado = 5
}

/// <summary>
/// Partner type.
/// </summary>
public enum TipoParceiro
{
    /// <summary>Gym partner.</summary>
    Academia = 1,

    /// <summary>Nutrition professional partner.</summary>
    Nutricionista = 2,

    /// <summary>Personal trainer partner.</summary>
    PersonalTrainer = 3,

    /// <summary>Company partner.</summary>
    Empresa = 4,

    /// <summary>Affiliate partner.</summary>
    Afiliado = 5
}

/// <summary>
/// Partner status.
/// </summary>
public enum StatusParceiro
{
    /// <summary>Draft partner.</summary>
    Rascunho = 1,

    /// <summary>Partner under review.</summary>
    EmAnalise = 2,

    /// <summary>Approved partner.</summary>
    Aprovado = 3,

    /// <summary>Suspended partner.</summary>
    Suspenso = 4,

    /// <summary>Removed partner.</summary>
    Removido = 5
}

/// <summary>
/// Notification status.
/// </summary>
public enum StatusNotificacao
{
    /// <summary>Pending notification.</summary>
    Pendente = 1,

    /// <summary>Sent notification.</summary>
    Enviada = 2,

    /// <summary>Read notification.</summary>
    Lida = 3,

    /// <summary>Failed notification.</summary>
    Falhou = 4
}

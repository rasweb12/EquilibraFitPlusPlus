namespace EquilibraFitPlusPlus.Infrastructure.AdminBootstrap;

public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";
    public bool Enabled { get; set; }
    public Guid SupabaseUserId { get; set; }
}

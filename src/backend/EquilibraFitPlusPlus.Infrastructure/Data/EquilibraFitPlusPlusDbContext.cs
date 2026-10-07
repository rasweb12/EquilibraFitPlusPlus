using System.Linq.Expressions;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql;

namespace EquilibraFitPlusPlus.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for EquilibraFit++.
/// </summary>
public sealed class EquilibraFitPlusPlusDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// Initializes the database context.
    /// </summary>
    public EquilibraFitPlusPlusDbContext(DbContextOptions<EquilibraFitPlusPlusDbContext> options)
        : base(options)
    {
    }

    /// <summary>Tenants.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Business users.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    /// <summary>User consents.</summary>
    public DbSet<ConsentimentoUsuario> ConsentimentosUsuario => Set<ConsentimentoUsuario>();

    /// <summary>Health profiles.</summary>
    public DbSet<PerfilSaude> PerfisSaude => Set<PerfilSaude>();

    /// <summary>User plans.</summary>
    public DbSet<PlanoUsuario> PlanosUsuario => Set<PlanoUsuario>();

    /// <summary>Nutritional goals.</summary>
    public DbSet<MetaNutricional> MetasNutricionais => Set<MetaNutricional>();

    /// <summary>Food logs.</summary>
    public DbSet<RegistroAlimentar> RegistrosAlimentares => Set<RegistroAlimentar>();

    /// <summary>Food log items.</summary>
    public DbSet<ItemAlimentar> ItensAlimentares => Set<ItemAlimentar>();

    /// <summary>Meal image analyses.</summary>
    public DbSet<AnaliseRefeicaoImagem> AnalisesRefeicaoImagem => Set<AnaliseRefeicaoImagem>();

    /// <summary>Progress logs.</summary>
    public DbSet<RegistroEvolucao> RegistrosEvolucao => Set<RegistroEvolucao>();

    /// <summary>Daily habit logs.</summary>
    public DbSet<RegistroHabitos> RegistrosHabitos => Set<RegistroHabitos>();

    /// <summary>Body measurements.</summary>
    public DbSet<MedidaCorporal> MedidasCorporais => Set<MedidaCorporal>();

    /// <summary>Exercise catalog.</summary>
    public DbSet<Exercicio> Exercicios => Set<Exercicio>();

    /// <summary>User workouts.</summary>
    public DbSet<TreinoUsuario> TreinosUsuario => Set<TreinoUsuario>();

    /// <summary>User workout exercises.</summary>
    public DbSet<TreinoExercicio> TreinosExercicios => Set<TreinoExercicio>();

    /// <summary>User workout exercise completion logs.</summary>
    public DbSet<TreinoExercicioConclusao> TreinosExerciciosConclusoes => Set<TreinoExercicioConclusao>();

    /// <summary>Real workout execution sessions.</summary>
    public DbSet<TreinoSessao> TreinosSessoes => Set<TreinoSessao>();

    /// <summary>Performed workout sets.</summary>
    public DbSet<TreinoSerieRealizada> TreinosSeriesRealizadas => Set<TreinoSerieRealizada>();

    /// <summary>Workout evolution proposals.</summary>
    public DbSet<TreinoEvolucaoProposta> TreinosEvolucoesPropostas => Set<TreinoEvolucaoProposta>();

    /// <summary>Workout progression suggestions.</summary>
    public DbSet<TreinoProgressaoSugestao> TreinosProgressoesSugestoes => Set<TreinoProgressaoSugestao>();

    /// <summary>Predefined meals.</summary>
    public DbSet<RefeicaoPredefinida> RefeicoesPredefinidas => Set<RefeicaoPredefinida>();

    /// <summary>Predefined meal items.</summary>
    public DbSet<ItemRefeicaoPredefinida> ItensRefeicaoPredefinida => Set<ItemRefeicaoPredefinida>();

    /// <summary>Subscriptions.</summary>
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();

    public DbSet<BillingEvent> BillingEvents => Set<BillingEvent>();

    /// <summary>Payments.</summary>
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    /// <summary>Coupons.</summary>
    public DbSet<Cupom> Cupons => Set<Cupom>();

    /// <summary>Partners.</summary>
    public DbSet<Parceiro> Parceiros => Set<Parceiro>();

    /// <summary>Professionals.</summary>
    public DbSet<Profissional> Profissionais => Set<Profissional>();

    /// <summary>Feature flags.</summary>
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    /// <summary>System settings.</summary>
    public DbSet<ConfiguracaoSistema> ConfiguracoesSistema => Set<ConfiguracaoSistema>();

    /// <summary>Audit logs.</summary>
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    /// <summary>Notifications.</summary>
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    /// <summary>Idempotent write response cache.</summary>
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    /// <summary>AI coach sessions.</summary>
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

    /// <summary>AI coach messages.</summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    /// <summary>AI coach personalization memory.</summary>
    public DbSet<AiCoachMemory> AiCoachMemories => Set<AiCoachMemory>();

    /// <summary>Sanitized AI execution logs.</summary>
    public DbSet<AiExecutionLog> AiExecutionLogs => Set<AiExecutionLog>();

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => await SaveChangesAsync(true, cancellationToken);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceConflictException(
                "persistence.concurrency",
                "Os dados foram alterados por outra operação.",
                exception);
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            throw new PersistenceConflictException(
                "persistence.unique_constraint",
                "Já existe outro registro ocupando esta posição.",
                exception);
        }
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            or SqliteException { SqliteExtendedErrorCode: 2067 or 1555 };
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("app");
        ConfigureDomain(builder);
        ConfigureConventions(builder);
        ConfigureProviderTypes(builder);
        SeedRequiredData(builder);
    }

    private static void ConfigureDomain(ModelBuilder builder)
    {
        builder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions");
            entity.HasIndex(x => x.SessionId).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.UsuarioId });
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Tipo).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.Nome);
        });

        builder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.HasIndex(x => x.IdentityUserId).IsUnique();
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SubstituidoPorTokenHash).HasMaxLength(128);
            entity.Property(x => x.CriadoPorIp).HasMaxLength(64);
            entity.Property(x => x.RevogadoPorIp).HasMaxLength(64);
            entity.Ignore(x => x.Ativo);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UsuarioId, x.ExpiraEm });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConsentimentoUsuario>(entity =>
        {
            entity.ToTable("ConsentimentosUsuario");
            entity.Property(x => x.Versao).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Origem).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.UsuarioId, x.Tipo, x.Versao }).IsUnique();
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PerfilSaude>(entity =>
        {
            entity.ToTable("PerfisSaude");
            entity.Property(x => x.AlturaCm).HasPrecision(5, 2);
            entity.Property(x => x.PesoAtualKg).HasPrecision(6, 2);
            entity.HasIndex(x => x.UsuarioId).IsUnique();
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithOne(x => x.PerfilSaude).HasForeignKey<PerfilSaude>(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_PerfisSaude_AlturaCm", "\"AlturaCm\" BETWEEN 80 AND 250"));
            entity.ToTable(t => t.HasCheckConstraint("CK_PerfisSaude_PesoAtualKg", "\"PesoAtualKg\" BETWEEN 25 AND 350"));
            entity.ToTable(t => t.HasCheckConstraint("CK_PerfisSaude_DiasTreinoSemana", "\"DiasTreinoSemana\" BETWEEN 0 AND 7"));
        });

        builder.Entity<PlanoUsuario>(entity =>
        {
            entity.ToTable("PlanosUsuario");
            entity.Property(x => x.ObjetivoSemanalKg).HasPrecision(4, 2);
            entity.Property(x => x.Explicacao).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.FonteGeracao).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ModeloIaVersao).HasMaxLength(80);
            entity.HasIndex(x => new { x.UsuarioId, x.Status });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MetaNutricional>(entity =>
        {
            entity.ToTable("MetasNutricionais");
            entity.Property(x => x.ProteinaG).HasPrecision(8, 2);
            entity.Property(x => x.CarboidratoG).HasPrecision(8, 2);
            entity.Property(x => x.GorduraG).HasPrecision(8, 2);
            entity.Property(x => x.FibraG).HasPrecision(8, 2);
            entity.HasQueryFilter("PlanoAtivo", x => x.PlanoUsuario == null || x.PlanoUsuario.ExcluidoEm == null);
        });

        ConfigureFood(builder);
        ConfigureProgressAndTraining(builder);
        ConfigureOperations(builder);
    }

    private static void ConfigureFood(ModelBuilder builder)
    {
        builder.Entity<RegistroAlimentar>(entity =>
        {
            entity.ToTable("RegistrosAlimentares");
            entity.Property(x => x.CaloriasTotal).HasPrecision(8, 2);
            entity.Property(x => x.ProteinaTotalG).HasPrecision(8, 2);
            entity.Property(x => x.CarboidratoTotalG).HasPrecision(8, 2);
            entity.Property(x => x.GorduraTotalG).HasPrecision(8, 2);
            entity.HasIndex(x => new { x.UsuarioId, x.DataHora });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ItemAlimentar>(entity =>
        {
            entity.ToTable("ItensAlimentares");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Unidade).HasMaxLength(40).IsRequired();
            entity.Property(x => x.FonteNutricional).HasMaxLength(40);
            entity.Property(x => x.Quantidade).HasPrecision(8, 2);
            entity.Property(x => x.Calorias).HasPrecision(8, 2);
            entity.Property(x => x.ProteinaG).HasPrecision(8, 2);
            entity.Property(x => x.CarboidratoG).HasPrecision(8, 2);
            entity.Property(x => x.GorduraG).HasPrecision(8, 2);
            entity.HasQueryFilter("RegistroAlimentarAtivo", x => x.RegistroAlimentar == null || x.RegistroAlimentar.ExcluidoEm == null);
        });

        builder.Entity<AnaliseRefeicaoImagem>(entity =>
        {
            entity.ToTable("AnalisesRefeicaoImagem");
            entity.Property(x => x.BlobUri).HasMaxLength(1024);
            entity.Property(x => x.ModeloVisao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ConfiancaMedia).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.UsuarioId, x.CriadoEm });
            entity.ToTable(t => t.HasCheckConstraint("CK_AnaliseRefeicaoImagem_Confianca", "\"ConfiancaMedia\" BETWEEN 0 AND 100"));
        });
    }

    private static void ConfigureProgressAndTraining(ModelBuilder builder)
    {
        builder.Entity<RegistroEvolucao>(entity =>
        {
            entity.ToTable("RegistrosEvolucao");
            entity.Property(x => x.PesoKg).HasPrecision(6, 2);
            entity.Property(x => x.PercentualGordura).HasPrecision(5, 2);
            entity.Property(x => x.PercentualMassaMagra).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.UsuarioId, x.Data }).IsUnique();
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RegistroHabitos>(entity =>
        {
            entity.ToTable("RegistrosHabitos");
            entity.Property(x => x.SonoHoras).HasPrecision(4, 2);
            entity.Property(x => x.MetaSonoHoras).HasPrecision(4, 2);
            entity.HasIndex(x => new { x.UsuarioId, x.Data }).IsUnique();
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_RegistrosHabitos_AguaMl", "\"AguaMl\" BETWEEN 0 AND 10000"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RegistrosHabitos_MetaAguaMl", "\"MetaAguaMl\" BETWEEN 250 AND 10000"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RegistrosHabitos_SonoHoras", "\"SonoHoras\" BETWEEN 0 AND 24"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RegistrosHabitos_MetaSonoHoras", "\"MetaSonoHoras\" BETWEEN 1 AND 14"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RegistrosHabitos_Humor", "\"Humor\" BETWEEN 1 AND 5"));
        });

        builder.Entity<MedidaCorporal>(entity =>
        {
            entity.ToTable("MedidasCorporais");
            entity.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ValorCm).HasPrecision(6, 2);
            entity.HasQueryFilter("RegistroEvolucaoAtivo", x => x.RegistroEvolucao == null || x.RegistroEvolucao.ExcluidoEm == null);
        });

        builder.Entity<Exercicio>(entity =>
        {
            entity.ToTable("Exercicios");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.GrupoMuscular).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Nivel).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Equipamento).HasMaxLength(120);
        });

        builder.Entity<TreinoUsuario>(entity =>
        {
            entity.ToTable("TreinosUsuario");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Objetivo).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Fase).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MotivoFinalizacao).HasMaxLength(240);
            entity.HasIndex(x => new { x.UsuarioId, x.Ativo });
            entity.HasIndex(x => new { x.UsuarioId, x.Versao }).IsUnique();
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TreinoAnterior).WithMany().HasForeignKey(x => x.TreinoAnteriorId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosUsuario_FrequenciaSemanal", "\"FrequenciaSemanal\" BETWEEN 1 AND 7"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosUsuario_DuracaoSemanas", "\"DuracaoSemanas\" BETWEEN 1 AND 52"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosUsuario_Versao", "\"Versao\" >= 1"));
        });

        builder.Entity<TreinoExercicio>(entity =>
        {
            entity.ToTable("TreinosExercicios");

            entity.Property(x => x.Repeticoes)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(x => x.Observacao)
                .HasMaxLength(1000);

            entity.Property(x => x.ProgressaoMotivo)
                .HasMaxLength(1000);

            entity.Property(x => x.CargaAlvoKg)
                .HasPrecision(7, 2);

            entity.HasIndex(
                    x => new
                    {
                        x.TreinoUsuarioId,
                        x.DiaTreino,
                        x.Ordem,
                    })
                .IsUnique()
                .HasFilter("\"ExcluidoEm\" IS NULL");

            entity.HasQueryFilter(
                "TreinoAtivo",
                x =>
                    x.TreinoUsuario == null ||
                    x.TreinoUsuario.ExcluidoEm == null);

            entity.HasQueryFilter(
                "TreinoExercicioAtivo",
                x => x.ExcluidoEm == null);

            entity.HasOne(x => x.TreinoUsuario)
                .WithMany(x => x.Exercicios)
                .HasForeignKey(x => x.TreinoUsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Exercicio)
                .WithMany()
                .HasForeignKey(x => x.ExercicioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_DiaTreino",
                    "\"DiaTreino\" BETWEEN 1 AND 7"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_Series",
                    "\"Series\" BETWEEN 1 AND 20"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_DescansoSegundos",
                    "\"DescansoSegundos\" BETWEEN 0 AND 900"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_CargaAlvoKg",
                    "\"CargaAlvoKg\" IS NULL OR \"CargaAlvoKg\" BETWEEN 0 AND 1000"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_RpeAlvo",
                    "\"RpeAlvo\" IS NULL OR \"RpeAlvo\" BETWEEN 1 AND 10"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_RepeticoesMin",
                    "\"RepeticoesMin\" IS NULL OR \"RepeticoesMin\" BETWEEN 0 AND 200"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_RepeticoesMax",
                    "\"RepeticoesMax\" IS NULL OR \"RepeticoesMax\" BETWEEN 0 AND 200"));

            entity.ToTable(
                t => t.HasCheckConstraint(
                    "CK_TreinosExercicios_RepeticoesRange",
                    "\"RepeticoesMin\" IS NULL OR "
                    + "\"RepeticoesMax\" IS NULL OR "
                    + "\"RepeticoesMin\" <= \"RepeticoesMax\""));
        });

        builder.Entity<TreinoExercicioConclusao>(entity =>
        {
            entity.ToTable("TreinosExerciciosConclusoes");
            entity.HasIndex(x => new { x.TenantId, x.UsuarioId, x.TreinoExercicioId, x.Data }).IsUnique();
            entity.HasIndex(x => new { x.TreinoUsuarioId, x.Data });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TreinoUsuario).WithMany().HasForeignKey(x => x.TreinoUsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TreinoExercicio).WithMany().HasForeignKey(x => x.TreinoExercicioId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TreinoSessao>(entity =>
        {
            entity.ToTable("TreinosSessoes");
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.Property(x => x.Resumo).HasMaxLength(2000);
            entity.HasIndex(x => new { x.UsuarioId, x.Data });
            entity.HasIndex(x => new { x.TreinoUsuarioId, x.Data });
            entity.HasIndex(x => new { x.TenantId, x.UsuarioId, x.OperationId })
                .IsUnique()
                .HasFilter("\"OperationId\" IS NOT NULL");
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TreinoUsuario).WithMany(x => x.Sessoes).HasForeignKey(x => x.TreinoUsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosSessoes_DiaTreino", "\"DiaTreino\" BETWEEN 1 AND 7"));
        });

        builder.Entity<TreinoSerieRealizada>(entity =>
        {
            entity.ToTable("TreinosSeriesRealizadas");
            entity.Property(x => x.CargaKg).HasPrecision(7, 2);
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.Property(x => x.DorDescricao).HasMaxLength(1000);
            entity.HasIndex(x => new { x.TreinoExercicioId, x.CriadoEm });
            entity.HasOne(x => x.Sessao).WithMany(x => x.Series).HasForeignKey(x => x.TreinoSessaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TreinoExercicio).WithMany(x => x.SeriesRealizadas).HasForeignKey(x => x.TreinoExercicioId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosSeriesRealizadas_NumeroSerie", "\"NumeroSerie\" BETWEEN 1 AND 20"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosSeriesRealizadas_CargaKg", "\"CargaKg\" IS NULL OR \"CargaKg\" BETWEEN 0 AND 1000"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosSeriesRealizadas_Repeticoes", "\"RepeticoesRealizadas\" BETWEEN 0 AND 200"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosSeriesRealizadas_Rpe", "\"Rpe\" BETWEEN 1 AND 10"));
        });

        builder.Entity<TreinoEvolucaoProposta>(entity =>
        {
            entity.ToTable("TreinosEvolucoesPropostas");
            entity.Property(x => x.PlanoJson).HasMaxLength(12000).IsRequired();
            entity.Property(x => x.MudancasResumo).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.UsuarioId, x.Status, x.CriadoEm });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TreinoUsuario).WithMany(x => x.PropostasEvolucao).HasForeignKey(x => x.TreinoUsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosEvolucoesPropostas_VersaoProposta", "\"VersaoProposta\" >= 1"));
        });

        builder.Entity<TreinoProgressaoSugestao>(entity =>
        {
            entity.ToTable("TreinosProgressoesSugestoes");
            entity.Property(x => x.CargaAtualKg).HasPrecision(7, 2);
            entity.Property(x => x.CargaSugeridaKg).HasPrecision(7, 2);
            entity.Property(x => x.Motivo).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.UsuarioId, x.Status, x.CriadoEm });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TreinoUsuario).WithMany().HasForeignKey(x => x.TreinoUsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.TreinoExercicio).WithMany().HasForeignKey(x => x.TreinoExercicioId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosProgressoesSugestoes_CargaAtualKg", "\"CargaAtualKg\" IS NULL OR \"CargaAtualKg\" BETWEEN 0 AND 1000"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosProgressoesSugestoes_CargaSugeridaKg", "\"CargaSugeridaKg\" IS NULL OR \"CargaSugeridaKg\" BETWEEN 0 AND 1000"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosProgressoesSugestoes_RpeAlvo", "\"RpeAlvo\" IS NULL OR \"RpeAlvo\" BETWEEN 1 AND 10"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosProgressoesSugestoes_RepeticoesMin", "\"RepeticoesMin\" IS NULL OR \"RepeticoesMin\" BETWEEN 0 AND 200"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosProgressoesSugestoes_RepeticoesMax", "\"RepeticoesMax\" IS NULL OR \"RepeticoesMax\" BETWEEN 0 AND 200"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TreinosProgressoesSugestoes_RepeticoesRange", "\"RepeticoesMin\" IS NULL OR \"RepeticoesMax\" IS NULL OR \"RepeticoesMin\" <= \"RepeticoesMax\""));
        });
    }

    private static void ConfigureOperations(ModelBuilder builder)
    {
        builder.Entity<RefeicaoPredefinida>(entity =>
        {
            entity.ToTable("RefeicoesPredefinidas");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
        });

        builder.Entity<ItemRefeicaoPredefinida>(entity =>
        {
            entity.ToTable("ItensRefeicaoPredefinida");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Unidade).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Quantidade).HasPrecision(8, 2);
        });

        builder.Entity<Assinatura>(entity =>
        {
            entity.ToTable("Assinaturas");
            entity.Property(x => x.PlanoCodigo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ProviderId).HasMaxLength(160);
            entity.Property(x => x.Plataforma).HasMaxLength(40);
            entity.Property(x => x.ProductId).HasMaxLength(160);
            entity.Property(x => x.PurchaseTokenHash).HasMaxLength(64);
            entity.Property(x => x.PurchaseTokenEncrypted).HasMaxLength(6000);
            entity.Property(x => x.OrderId).HasMaxLength(160);
            entity.Property(x => x.EstadoCompra).HasMaxLength(80);
            entity.Property(x => x.UltimoEventoId).HasMaxLength(160);
            entity.HasIndex(x => x.PurchaseTokenHash).IsUnique().HasFilter("\"PurchaseTokenHash\" IS NOT NULL");
            entity.HasIndex(x => new { x.UsuarioId, x.Status });
        });

        builder.Entity<BillingEvent>(entity =>
        {
            entity.ToTable("BillingEvents");
            entity.Property(x => x.EventId).HasMaxLength(160).IsRequired();
            entity.Property(x => x.State).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasOne(x => x.Assinatura).WithMany().HasForeignKey(x => x.AssinaturaId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Pagamento>(entity =>
        {
            entity.ToTable("Pagamentos");
            entity.Property(x => x.Provider).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Metodo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Moeda).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Valor).HasPrecision(10, 2);
            entity.Property(x => x.TransacaoExternaId).HasMaxLength(160);
        });

        builder.Entity<Cupom>(entity =>
        {
            entity.ToTable("Cupons");
            entity.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PercentualDesconto).HasPrecision(5, 2);
            entity.Property(x => x.ValorDesconto).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique();
        });

        builder.Entity<Parceiro>(entity =>
        {
            entity.ToTable("Parceiros");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EmailContato).HasMaxLength(256);
            entity.Property(x => x.Documento).HasMaxLength(32);
        });

        builder.Entity<Profissional>(entity =>
        {
            entity.ToTable("Profissionais");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.RegistroProfissional).HasMaxLength(80);
            entity.Property(x => x.Especialidade).HasMaxLength(120);
        });

        builder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags");
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Chave }).IsUnique();
        });

        builder.Entity<ConfiguracaoSistema>(entity =>
        {
            entity.ToTable("ConfiguracoesSistema");
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Valor).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Chave }).IsUnique();
        });

        builder.Entity<Auditoria>(entity =>
        {
            entity.ToTable("Auditorias");
            entity.Property(x => x.Acao).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Entidade).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Ip).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.HasIndex(x => new { x.Entidade, x.CriadoEm });
        });

        builder.Entity<Notificacao>(entity =>
        {
            entity.ToTable("Notificacoes");
            entity.Property(x => x.Titulo).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Mensagem).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.UsuarioId, x.Status, x.CriadoEm });
        });

        builder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("IdempotencyRecords");
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Metodo).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Caminho).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ResponseContentType).HasMaxLength(160);
            entity.Property(x => x.ResponseBody).HasMaxLength(20000);
            entity.HasIndex(x => new { x.TenantId, x.UsuarioId, x.Chave, x.Metodo, x.Caminho }).IsUnique();
            entity.HasIndex(x => x.ExpiresAt);
        });

        builder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("ChatSessions");
            entity.Property(x => x.Titulo).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.UsuarioId, x.CriadoEm });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.Property(x => x.Role).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Conteudo).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.ModeloIa).HasMaxLength(80);
            entity.HasIndex(x => new { x.ChatSessionId, x.CriadoEm });
            entity.HasQueryFilter("ChatSessionAtiva", x => x.ChatSession == null || x.ChatSession.ExcluidoEm == null);
            entity.HasOne(x => x.ChatSession).WithMany(x => x.Mensagens).HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_ChatMessages_Conteudo_Length", "length(\"Conteudo\") <= 8000"));
        });

        builder.Entity<AiCoachMemory>(entity =>
        {
            entity.ToTable("AiCoachMemories");
            entity.Property(x => x.Categoria).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Valor).HasMaxLength(240).IsRequired();
            entity.Property(x => x.TipoFato).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Fonte).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.UsuarioId, x.Chave }).IsUnique();
            entity.HasIndex(x => new { x.UsuarioId, x.Categoria });
            entity.HasQueryFilter("UsuarioAtivo", x => x.Usuario == null || x.Usuario.ExcluidoEm == null);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_AiCoachMemories_Valor_Length", "length(\"Valor\") <= 240"));
        });

        builder.Entity<AiExecutionLog>(entity =>
        {
            entity.ToTable("AiExecutionLogs");
            entity.Property(x => x.UsuarioIdHash).HasMaxLength(96).IsRequired();
            entity.Property(x => x.Operation).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PromptName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PromptVersion).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RetrievedDocumentIdsJson).HasMaxLength(2000);
            entity.Property(x => x.EstimatedCost).HasPrecision(12, 6);
            entity.Property(x => x.Confidence).HasPrecision(5, 4);
            entity.Property(x => x.SafetyResult).HasMaxLength(240);
            entity.Property(x => x.CorrelationId).HasMaxLength(120);
            entity.HasIndex(x => new { x.TenantId, x.Operation, x.CriadoEm });
            entity.HasIndex(x => new { x.TenantId, x.UsuarioIdHash, x.CriadoEm });
        });
    }

    private static void ConfigureConventions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            Type clrType = entityType.ClrType;

            if (typeof(AuditableEntity).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType).Property(nameof(AuditableEntity.RowVersion))
                    .IsConcurrencyToken().ValueGeneratedNever();
            }

            if (typeof(ISoftDelete).IsAssignableFrom(clrType))
            {
                ParameterExpression parameter = Expression.Parameter(clrType, "entity");
                MemberExpression property = Expression.Property(parameter, nameof(ISoftDelete.ExcluidoEm));
                BinaryExpression body = Expression.Equal(property, Expression.Constant(null, typeof(DateTimeOffset?)));
                builder.Entity(clrType).HasQueryFilter("SoftDelete", Expression.Lambda(body, parameter));
            }
        }
    }

    private static void SeedRequiredData(ModelBuilder builder)
    {
        builder.Entity<Tenant>().HasData(new
        {
            Id = SeedData.DefaultTenantId,
            Nome = "EquilibraFit++ Individual",
            Tipo = "Individual",
            Ativo = true,
            CriadoEm = new DateTimeOffset(2026, 7, 31, 0, 0, 0, TimeSpan.Zero),
            RowVersion = Array.Empty<byte>()
        });

    }

    private void ApplyAuditInformation()
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = utcNow;
            }
        }
    }

    public override int SaveChanges() => SaveChanges(true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        try { return base.SaveChanges(acceptAllChangesOnSuccess); }
        catch (DbUpdateConcurrencyException ex)
        { throw new PersistenceConflictException("persistence.concurrency", "Os dados foram alterados por outra operacao.", ex); }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        { throw new PersistenceConflictException("persistence.unique_constraint", "Registro duplicado.", ex); }
    }

    private void ConfigureProviderTypes(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) != typeof(DateTimeOffset))
                {
                    continue;
                }

                if (Database.IsSqlite())
                {
                    property.SetValueConverter(new ValueConverter<DateTimeOffset, long>(
                        value => value.UtcTicks,
                        value => new DateTimeOffset(value, TimeSpan.Zero)));
                }
                else if (Database.IsNpgsql())
                {
                    property.SetValueConverter(new ValueConverter<DateTimeOffset, DateTimeOffset>(
                        value => value.ToUniversalTime(),
                        value => value));
                }
            }
        }
    }
}

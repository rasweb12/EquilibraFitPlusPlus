using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EquilibraFitPlusPlus.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "AiExecutionLogs",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioIdHash = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Operation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PromptName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContextVersion = table.Column<int>(type: "integer", nullable: false),
                    RetrievalUsed = table.Column<bool>(type: "boolean", nullable: false),
                    RetrievedDocumentIdsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FallbackUsed = table.Column<bool>(type: "boolean", nullable: false),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(12,6)", precision: 12, scale: 6, nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    SafetyResult = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assinaturas",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoCodigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InicioEm = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminaEm = table.Column<DateOnly>(type: "date", nullable: true),
                    ProviderId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auditorias",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Acao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Entidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadadosJson = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesSistema",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Sensivel = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cupons",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ValorDesconto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ExpiraEm = table.Column<DateOnly>(type: "date", nullable: true),
                    UsoMaximo = table.Column<int>(type: "integer", nullable: true),
                    UsoAtual = table.Column<int>(type: "integer", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exercicios",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    GrupoMuscular = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nivel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Equipamento = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Instrucao = table.Column<string>(type: "text", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Habilitada = table.Column<bool>(type: "boolean", nullable: false),
                    ConfiguracaoJson = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Metodo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Caminho = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseContentType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ResponseBody = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pagamentos",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssinaturaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Metodo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransacaoExternaId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parceiros",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EmailContato = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Documento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parceiros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParceiroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RegistroProfissional = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Especialidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefeicoesPredefinidas",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Publicada = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefeicoesPredefinidas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensRefeicaoPredefinida",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefeicaoPredefinidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensRefeicaoPredefinida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensRefeicaoPredefinida_RefeicoesPredefinidas_RefeicaoPred~",
                        column: x => x.RefeicaoPredefinidaId,
                        principalSchema: "app",
                        principalTable: "RefeicoesPredefinidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    UltimoLoginEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "app",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiCoachMemories",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Chave = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    TipoFato = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ConfirmadoPeloUsuario = table.Column<bool>(type: "boolean", nullable: false),
                    Fonte = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ConfirmadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCoachMemories", x => x.Id);
                    table.CheckConstraint("CK_AiCoachMemories_Valor_Length", "length(\"Valor\") <= 240");
                    table.ForeignKey(
                        name: "FK_AiCoachMemories_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsentimentosUsuario",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Versao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AceitoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Origem = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentimentosUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentimentosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfisSaude",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    SexoBiologico = table.Column<int>(type: "integer", nullable: false),
                    AlturaCm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PesoAtualKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Objetivo = table.Column<int>(type: "integer", nullable: false),
                    NivelAtividade = table.Column<int>(type: "integer", nullable: false),
                    DiasTreinoSemana = table.Column<byte>(type: "smallint", nullable: false),
                    PreferenciasJson = table.Column<string>(type: "text", nullable: true),
                    RestricoesJson = table.Column<string>(type: "text", nullable: true),
                    ObservacoesJson = table.Column<string>(type: "text", nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisSaude", x => x.Id);
                    table.CheckConstraint("CK_PerfisSaude_AlturaCm", "\"AlturaCm\" BETWEEN 80 AND 250");
                    table.CheckConstraint("CK_PerfisSaude_DiasTreinoSemana", "\"DiasTreinoSemana\" BETWEEN 0 AND 7");
                    table.CheckConstraint("CK_PerfisSaude_PesoAtualKg", "\"PesoAtualKg\" BETWEEN 25 AND 350");
                    table.ForeignKey(
                        name: "FK_PerfisSaude_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanosUsuario",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CaloriasDia = table.Column<int>(type: "integer", nullable: false),
                    ObjetivoSemanalKg = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    Explicacao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FonteGeracao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ModeloIaVersao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevogadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubstituidoPorTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CriadoPorIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RevogadoPorIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAlimentares",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TipoRefeicao = table.Column<int>(type: "integer", nullable: false),
                    Origem = table.Column<int>(type: "integer", nullable: false),
                    CaloriasTotal = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ProteinaTotalG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CarboidratoTotalG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    GorduraTotalG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ConfirmadoPeloUsuario = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAlimentares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAlimentares_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosEvolucao",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    PesoKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    PercentualGordura = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PercentualMassaMagra = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosEvolucao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosEvolucao_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosHabitos",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    AguaMl = table.Column<int>(type: "integer", nullable: false),
                    MetaAguaMl = table.Column<int>(type: "integer", nullable: false),
                    SonoHoras = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    MetaSonoHoras = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    Humor = table.Column<int>(type: "integer", nullable: false),
                    MeditacaoRealizada = table.Column<bool>(type: "boolean", nullable: false),
                    AlongamentoRealizado = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosHabitos", x => x.Id);
                    table.CheckConstraint("CK_RegistrosHabitos_AguaMl", "\"AguaMl\" BETWEEN 0 AND 10000");
                    table.CheckConstraint("CK_RegistrosHabitos_Humor", "\"Humor\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_RegistrosHabitos_MetaAguaMl", "\"MetaAguaMl\" BETWEEN 250 AND 10000");
                    table.CheckConstraint("CK_RegistrosHabitos_MetaSonoHoras", "\"MetaSonoHoras\" BETWEEN 1 AND 14");
                    table.CheckConstraint("CK_RegistrosHabitos_SonoHoras", "\"SonoHoras\" BETWEEN 0 AND 24");
                    table.ForeignKey(
                        name: "FK_RegistrosHabitos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreinosUsuario",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FrequenciaSemanal = table.Column<byte>(type: "smallint", nullable: false),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    TreinoAnteriorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    DuracaoSemanas = table.Column<int>(type: "integer", nullable: false),
                    Fase = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MotivoFinalizacao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosUsuario", x => x.Id);
                    table.CheckConstraint("CK_TreinosUsuario_DuracaoSemanas", "\"DuracaoSemanas\" BETWEEN 1 AND 52");
                    table.CheckConstraint("CK_TreinosUsuario_FrequenciaSemanal", "\"FrequenciaSemanal\" BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_TreinosUsuario_Versao", "\"Versao\" >= 1");
                    table.ForeignKey(
                        name: "FK_TreinosUsuario_TreinosUsuario_TreinoAnteriorId",
                        column: x => x.TreinoAnteriorId,
                        principalSchema: "app",
                        principalTable: "TreinosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreinosUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Conteudo = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ModeloIa = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.CheckConstraint("CK_ChatMessages_Conteudo_Length", "length(\"Conteudo\") <= 8000");
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalSchema: "app",
                        principalTable: "ChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetasNutricionais",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProteinaG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CarboidratoG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    GorduraG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    FibraG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    AguaMl = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasNutricionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetasNutricionais_PlanosUsuario_PlanoUsuarioId",
                        column: x => x.PlanoUsuarioId,
                        principalSchema: "app",
                        principalTable: "PlanosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalisesRefeicaoImagem",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistroAlimentarId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlobUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ConfiancaMedia = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ModeloVisao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResultadoJson = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalisesRefeicaoImagem", x => x.Id);
                    table.CheckConstraint("CK_AnaliseRefeicaoImagem_Confianca", "\"ConfiancaMedia\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_AnalisesRefeicaoImagem_RegistrosAlimentares_RegistroAliment~",
                        column: x => x.RegistroAlimentarId,
                        principalSchema: "app",
                        principalTable: "RegistrosAlimentares",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ItensAlimentares",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistroAlimentarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Calorias = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ProteinaG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CarboidratoG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    GorduraG = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    FonteNutricional = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensAlimentares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensAlimentares_RegistrosAlimentares_RegistroAlimentarId",
                        column: x => x.RegistroAlimentarId,
                        principalSchema: "app",
                        principalTable: "RegistrosAlimentares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedidasCorporais",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistroEvolucaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ValorCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedidasCorporais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedidasCorporais_RegistrosEvolucao_RegistroEvolucaoId",
                        column: x => x.RegistroEvolucaoId,
                        principalSchema: "app",
                        principalTable: "RegistrosEvolucao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreinosEvolucoesPropostas",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersaoProposta = table.Column<int>(type: "integer", nullable: false),
                    PlanoJson = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: false),
                    MudancasResumo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AplicadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosEvolucoesPropostas", x => x.Id);
                    table.CheckConstraint("CK_TreinosEvolucoesPropostas_VersaoProposta", "\"VersaoProposta\" >= 1");
                    table.ForeignKey(
                        name: "FK_TreinosEvolucoesPropostas_TreinosUsuario_TreinoUsuarioId",
                        column: x => x.TreinoUsuarioId,
                        principalSchema: "app",
                        principalTable: "TreinosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreinosEvolucoesPropostas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreinosExercicios",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    DiaTreino = table.Column<byte>(type: "smallint", nullable: false),
                    Series = table.Column<int>(type: "integer", nullable: false),
                    Repeticoes = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DescansoSegundos = table.Column<int>(type: "integer", nullable: false),
                    CargaAlvoKg = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    RpeAlvo = table.Column<byte>(type: "smallint", nullable: true),
                    RepeticoesMin = table.Column<int>(type: "integer", nullable: true),
                    RepeticoesMax = table.Column<int>(type: "integer", nullable: true),
                    ProgressaoMotivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExcluidoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoExclusao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosExercicios", x => x.Id);
                    table.CheckConstraint("CK_TreinosExercicios_CargaAlvoKg", "\"CargaAlvoKg\" IS NULL OR \"CargaAlvoKg\" BETWEEN 0 AND 1000");
                    table.CheckConstraint("CK_TreinosExercicios_DescansoSegundos", "\"DescansoSegundos\" BETWEEN 0 AND 900");
                    table.CheckConstraint("CK_TreinosExercicios_DiaTreino", "\"DiaTreino\" BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_TreinosExercicios_RepeticoesMax", "\"RepeticoesMax\" IS NULL OR \"RepeticoesMax\" BETWEEN 0 AND 200");
                    table.CheckConstraint("CK_TreinosExercicios_RepeticoesMin", "\"RepeticoesMin\" IS NULL OR \"RepeticoesMin\" BETWEEN 0 AND 200");
                    table.CheckConstraint("CK_TreinosExercicios_RepeticoesRange", "\"RepeticoesMin\" IS NULL OR \"RepeticoesMax\" IS NULL OR \"RepeticoesMin\" <= \"RepeticoesMax\"");
                    table.CheckConstraint("CK_TreinosExercicios_RpeAlvo", "\"RpeAlvo\" IS NULL OR \"RpeAlvo\" BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_TreinosExercicios_Series", "\"Series\" BETWEEN 1 AND 20");
                    table.ForeignKey(
                        name: "FK_TreinosExercicios_Exercicios_ExercicioId",
                        column: x => x.ExercicioId,
                        principalSchema: "app",
                        principalTable: "Exercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreinosExercicios_TreinosUsuario_TreinoUsuarioId",
                        column: x => x.TreinoUsuarioId,
                        principalSchema: "app",
                        principalTable: "TreinosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreinosSessoes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiaTreino = table.Column<byte>(type: "smallint", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    IniciadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinalizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Resumo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosSessoes", x => x.Id);
                    table.CheckConstraint("CK_TreinosSessoes_DiaTreino", "\"DiaTreino\" BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_TreinosSessoes_TreinosUsuario_TreinoUsuarioId",
                        column: x => x.TreinoUsuarioId,
                        principalSchema: "app",
                        principalTable: "TreinosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreinosSessoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreinosExerciciosConclusoes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoExercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Concluido = table.Column<bool>(type: "boolean", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosExerciciosConclusoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreinosExerciciosConclusoes_TreinosExercicios_TreinoExercic~",
                        column: x => x.TreinoExercicioId,
                        principalSchema: "app",
                        principalTable: "TreinosExercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreinosExerciciosConclusoes_TreinosUsuario_TreinoUsuarioId",
                        column: x => x.TreinoUsuarioId,
                        principalSchema: "app",
                        principalTable: "TreinosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreinosExerciciosConclusoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreinosProgressoesSugestoes",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoExercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CargaAtualKg = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    CargaSugeridaKg = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    RpeAlvo = table.Column<byte>(type: "smallint", nullable: true),
                    RepeticoesMin = table.Column<int>(type: "integer", nullable: true),
                    RepeticoesMax = table.Column<int>(type: "integer", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DecididaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosProgressoesSugestoes", x => x.Id);
                    table.CheckConstraint("CK_TreinosProgressoesSugestoes_CargaAtualKg", "\"CargaAtualKg\" IS NULL OR \"CargaAtualKg\" BETWEEN 0 AND 1000");
                    table.CheckConstraint("CK_TreinosProgressoesSugestoes_CargaSugeridaKg", "\"CargaSugeridaKg\" IS NULL OR \"CargaSugeridaKg\" BETWEEN 0 AND 1000");
                    table.CheckConstraint("CK_TreinosProgressoesSugestoes_RepeticoesMax", "\"RepeticoesMax\" IS NULL OR \"RepeticoesMax\" BETWEEN 0 AND 200");
                    table.CheckConstraint("CK_TreinosProgressoesSugestoes_RepeticoesMin", "\"RepeticoesMin\" IS NULL OR \"RepeticoesMin\" BETWEEN 0 AND 200");
                    table.CheckConstraint("CK_TreinosProgressoesSugestoes_RepeticoesRange", "\"RepeticoesMin\" IS NULL OR \"RepeticoesMax\" IS NULL OR \"RepeticoesMin\" <= \"RepeticoesMax\"");
                    table.CheckConstraint("CK_TreinosProgressoesSugestoes_RpeAlvo", "\"RpeAlvo\" IS NULL OR \"RpeAlvo\" BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_TreinosProgressoesSugestoes_TreinosExercicios_TreinoExercic~",
                        column: x => x.TreinoExercicioId,
                        principalSchema: "app",
                        principalTable: "TreinosExercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreinosProgressoesSugestoes_TreinosUsuario_TreinoUsuarioId",
                        column: x => x.TreinoUsuarioId,
                        principalSchema: "app",
                        principalTable: "TreinosUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreinosProgressoesSugestoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "app",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreinosSeriesRealizadas",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoSessaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreinoExercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroSerie = table.Column<int>(type: "integer", nullable: false),
                    CargaKg = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    RepeticoesRealizadas = table.Column<int>(type: "integer", nullable: false),
                    Rpe = table.Column<byte>(type: "smallint", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DorDesconforto = table.Column<bool>(type: "boolean", nullable: false),
                    DorDescricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CriadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AtualizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreinosSeriesRealizadas", x => x.Id);
                    table.CheckConstraint("CK_TreinosSeriesRealizadas_CargaKg", "\"CargaKg\" IS NULL OR \"CargaKg\" BETWEEN 0 AND 1000");
                    table.CheckConstraint("CK_TreinosSeriesRealizadas_NumeroSerie", "\"NumeroSerie\" BETWEEN 1 AND 20");
                    table.CheckConstraint("CK_TreinosSeriesRealizadas_Repeticoes", "\"RepeticoesRealizadas\" BETWEEN 0 AND 200");
                    table.CheckConstraint("CK_TreinosSeriesRealizadas_Rpe", "\"Rpe\" BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_TreinosSeriesRealizadas_TreinosExercicios_TreinoExercicioId",
                        column: x => x.TreinoExercicioId,
                        principalSchema: "app",
                        principalTable: "TreinosExercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreinosSeriesRealizadas_TreinosSessoes_TreinoSessaoId",
                        column: x => x.TreinoSessaoId,
                        principalSchema: "app",
                        principalTable: "TreinosSessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), "seed-role-usuario-v1", "Usuario", "USUARIO" },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "seed-role-administrador-v1", "Administrador", "ADMINISTRADOR" },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "seed-role-suporte-v1", "Suporte", "SUPORTE" },
                    { new Guid("22222222-2222-2222-2222-222222222204"), "seed-role-conteudo-v1", "Conteudo", "CONTEUDO" },
                    { new Guid("22222222-2222-2222-2222-222222222205"), "seed-role-operacaoia-v1", "OperacaoIa", "OPERACAOIA" },
                    { new Guid("22222222-2222-2222-2222-222222222206"), "seed-role-financeiro-v1", "Financeiro", "FINANCEIRO" }
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "Tenants",
                columns: new[] { "Id", "Ativo", "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor", "ExcluidoEm", "ExcluidoPor", "MotivoExclusao", "Nome", "RowVersion", "Tipo" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), true, null, null, new DateTimeOffset(new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "EquilibraFit++ Individual", new byte[0], "Individual" });

            migrationBuilder.CreateIndex(
                name: "IX_AiCoachMemories_TenantId_UsuarioId_Chave",
                schema: "app",
                table: "AiCoachMemories",
                columns: new[] { "TenantId", "UsuarioId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiCoachMemories_UsuarioId_Categoria",
                schema: "app",
                table: "AiCoachMemories",
                columns: new[] { "UsuarioId", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "IX_AiExecutionLogs_TenantId_Operation_CriadoEm",
                schema: "app",
                table: "AiExecutionLogs",
                columns: new[] { "TenantId", "Operation", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_AiExecutionLogs_TenantId_UsuarioIdHash_CriadoEm",
                schema: "app",
                table: "AiExecutionLogs",
                columns: new[] { "TenantId", "UsuarioIdHash", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalisesRefeicaoImagem_RegistroAlimentarId",
                schema: "app",
                table: "AnalisesRefeicaoImagem",
                column: "RegistroAlimentarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalisesRefeicaoImagem_UsuarioId_CriadoEm",
                schema: "app",
                table: "AnalisesRefeicaoImagem",
                columns: new[] { "UsuarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_UsuarioId_Status",
                schema: "app",
                table: "Assinaturas",
                columns: new[] { "UsuarioId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Entidade_CriadoEm",
                schema: "app",
                table: "Auditorias",
                columns: new[] { "Entidade", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatSessionId_CriadoEm",
                schema: "app",
                table: "ChatMessages",
                columns: new[] { "ChatSessionId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UsuarioId_CriadoEm",
                schema: "app",
                table: "ChatSessions",
                columns: new[] { "UsuarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesSistema_TenantId_Chave",
                schema: "app",
                table: "ConfiguracoesSistema",
                columns: new[] { "TenantId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimentosUsuario_UsuarioId_Tipo_Versao",
                schema: "app",
                table: "ConsentimentosUsuario",
                columns: new[] { "UsuarioId", "Tipo", "Versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cupons_TenantId_Codigo",
                schema: "app",
                table: "Cupons",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_TenantId_Chave",
                schema: "app",
                table: "FeatureFlags",
                columns: new[] { "TenantId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresAt",
                schema: "app",
                table: "IdempotencyRecords",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_TenantId_UsuarioId_Chave_Metodo_Caminho",
                schema: "app",
                table: "IdempotencyRecords",
                columns: new[] { "TenantId", "UsuarioId", "Chave", "Metodo", "Caminho" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensAlimentares_RegistroAlimentarId",
                schema: "app",
                table: "ItensAlimentares",
                column: "RegistroAlimentarId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensRefeicaoPredefinida_RefeicaoPredefinidaId",
                schema: "app",
                table: "ItensRefeicaoPredefinida",
                column: "RefeicaoPredefinidaId");

            migrationBuilder.CreateIndex(
                name: "IX_MedidasCorporais_RegistroEvolucaoId",
                schema: "app",
                table: "MedidasCorporais",
                column: "RegistroEvolucaoId");

            migrationBuilder.CreateIndex(
                name: "IX_MetasNutricionais_PlanoUsuarioId",
                schema: "app",
                table: "MetasNutricionais",
                column: "PlanoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioId_Status_CriadoEm",
                schema: "app",
                table: "Notificacoes",
                columns: new[] { "UsuarioId", "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_PerfisSaude_UsuarioId",
                schema: "app",
                table: "PerfisSaude",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanosUsuario_UsuarioId_Status",
                schema: "app",
                table: "PlanosUsuario",
                columns: new[] { "UsuarioId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                schema: "app",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UsuarioId_ExpiraEm",
                schema: "app",
                table: "RefreshTokens",
                columns: new[] { "UsuarioId", "ExpiraEm" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAlimentares_UsuarioId_DataHora",
                schema: "app",
                table: "RegistrosAlimentares",
                columns: new[] { "UsuarioId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosEvolucao_UsuarioId_Data",
                schema: "app",
                table: "RegistrosEvolucao",
                columns: new[] { "UsuarioId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosHabitos_UsuarioId_Data",
                schema: "app",
                table: "RegistrosHabitos",
                columns: new[] { "UsuarioId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "identity",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Nome",
                schema: "app",
                table: "Tenants",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosEvolucoesPropostas_TreinoUsuarioId",
                schema: "app",
                table: "TreinosEvolucoesPropostas",
                column: "TreinoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosEvolucoesPropostas_UsuarioId_Status_CriadoEm",
                schema: "app",
                table: "TreinosEvolucoesPropostas",
                columns: new[] { "UsuarioId", "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosExercicios_ExercicioId",
                schema: "app",
                table: "TreinosExercicios",
                column: "ExercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosExercicios_TreinoUsuarioId_DiaTreino_Ordem",
                schema: "app",
                table: "TreinosExercicios",
                columns: new[] { "TreinoUsuarioId", "DiaTreino", "Ordem" },
                unique: true,
                filter: "\"ExcluidoEm\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosExerciciosConclusoes_TenantId_UsuarioId_TreinoExerci~",
                schema: "app",
                table: "TreinosExerciciosConclusoes",
                columns: new[] { "TenantId", "UsuarioId", "TreinoExercicioId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreinosExerciciosConclusoes_TreinoExercicioId",
                schema: "app",
                table: "TreinosExerciciosConclusoes",
                column: "TreinoExercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosExerciciosConclusoes_TreinoUsuarioId_Data",
                schema: "app",
                table: "TreinosExerciciosConclusoes",
                columns: new[] { "TreinoUsuarioId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosExerciciosConclusoes_UsuarioId",
                schema: "app",
                table: "TreinosExerciciosConclusoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosProgressoesSugestoes_TreinoExercicioId",
                schema: "app",
                table: "TreinosProgressoesSugestoes",
                column: "TreinoExercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosProgressoesSugestoes_TreinoUsuarioId",
                schema: "app",
                table: "TreinosProgressoesSugestoes",
                column: "TreinoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosProgressoesSugestoes_UsuarioId_Status_CriadoEm",
                schema: "app",
                table: "TreinosProgressoesSugestoes",
                columns: new[] { "UsuarioId", "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosSeriesRealizadas_TreinoExercicioId_CriadoEm",
                schema: "app",
                table: "TreinosSeriesRealizadas",
                columns: new[] { "TreinoExercicioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosSeriesRealizadas_TreinoSessaoId",
                schema: "app",
                table: "TreinosSeriesRealizadas",
                column: "TreinoSessaoId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosSessoes_TenantId_UsuarioId_OperationId",
                schema: "app",
                table: "TreinosSessoes",
                columns: new[] { "TenantId", "UsuarioId", "OperationId" },
                unique: true,
                filter: "\"OperationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosSessoes_TreinoUsuarioId_Data",
                schema: "app",
                table: "TreinosSessoes",
                columns: new[] { "TreinoUsuarioId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosSessoes_UsuarioId_Data",
                schema: "app",
                table: "TreinosSessoes",
                columns: new[] { "UsuarioId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosUsuario_TreinoAnteriorId",
                schema: "app",
                table: "TreinosUsuario",
                column: "TreinoAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_TreinosUsuario_UsuarioId_Ativo",
                schema: "app",
                table: "TreinosUsuario",
                columns: new[] { "UsuarioId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_TreinosUsuario_UsuarioId_Versao",
                schema: "app",
                table: "TreinosUsuario",
                columns: new[] { "UsuarioId", "Versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "identity",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "identity",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                schema: "identity",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdentityUserId",
                schema: "app",
                table: "Usuarios",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId_Email",
                schema: "app",
                table: "Usuarios",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCoachMemories",
                schema: "app");

            migrationBuilder.DropTable(
                name: "AiExecutionLogs",
                schema: "app");

            migrationBuilder.DropTable(
                name: "AnalisesRefeicaoImagem",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Assinaturas",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Auditorias",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ChatMessages",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ConfiguracoesSistema",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ConsentimentosUsuario",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Cupons",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FeatureFlags",
                schema: "app");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ItensAlimentares",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ItensRefeicaoPredefinida",
                schema: "app");

            migrationBuilder.DropTable(
                name: "MedidasCorporais",
                schema: "app");

            migrationBuilder.DropTable(
                name: "MetasNutricionais",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Notificacoes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Pagamentos",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Parceiros",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PerfisSaude",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Profissionais",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RegistrosHabitos",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "TreinosEvolucoesPropostas",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TreinosExerciciosConclusoes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TreinosProgressoesSugestoes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TreinosSeriesRealizadas",
                schema: "app");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ChatSessions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RegistrosAlimentares",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RefeicoesPredefinidas",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RegistrosEvolucao",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PlanosUsuario",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TreinosExercicios",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TreinosSessoes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Exercicios",
                schema: "app");

            migrationBuilder.DropTable(
                name: "TreinosUsuario",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "app");
        }
    }
}

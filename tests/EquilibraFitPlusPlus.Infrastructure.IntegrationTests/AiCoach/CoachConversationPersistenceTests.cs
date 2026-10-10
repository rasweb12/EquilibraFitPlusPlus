using System.Net;
using System.Text;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.AiCoach.Commands.EnviarMensagemCoach;
using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.AiCoach;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using EquilibraFitPlusPlus.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.AiCoach;

/// <summary>Relational regression coverage for continuing a Coach conversation.</summary>
public sealed class CoachConversationPersistenceTests
{
    /// <summary>Each reply is inserted without updating earlier message versions.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubsequentMessages_ShouldBeInsertedAndPreserveConversationHistory(bool fallback)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseSqlite(connection).Options;
        await using var db = new EquilibraFitPlusPlusDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var user = new Usuario
        {
            TenantId = SeedData.DefaultTenantId, IdentityUserId = Guid.NewGuid(),
            Nome = "Coach test", Email = "coach@example.test"
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        var transport = new CoachTransport(fallback);
        using var http = new HttpClient(transport) { BaseAddress = new Uri("https://ai.example.test") };
        var handler = new EnviarMensagemCoachCommandHandler(
            new AiCoachRepository(db), new EmptyContextBuilder(),
            new AiCoachHttpClient(http, Options.Create(new AiCoachOptions()), NullLogger<AiCoachHttpClient>.Instance),
            new EmptyInstructions(), new UsuarioRepository(db), db);
        Guid? sessionId = null;
        var firstVersions = new Dictionary<Guid, byte[]>();

        for (int i = 0; i < 3; i++)
        {
            db.ChangeTracker.Clear();
            var result = await handler.Handle(new(user.TenantId, user.Id,
                new EnviarMensagemCoachRequest(sessionId, $"Mensagem de teste {i}", i switch { 1 => "gemini", 2 => "openai", _ => null })), default);
            Assert.True(result.IsSuccess);
            sessionId ??= result.Value!.SessaoId;
            Assert.Equal(sessionId, result.Value!.SessaoId);
            Assert.Equal(fallback, result.Value.FallbackUsed);
            db.ChangeTracker.Clear();
            var messages = await db.ChatMessages.AsNoTracking().Where(x => x.ChatSessionId == sessionId).ToArrayAsync();
            Assert.Equal((i + 1) * 2, messages.Length);
            Assert.Contains(messages, x => x.Role == "user" && x.Conteudo == $"Mensagem de teste {i}");
            foreach (var message in messages)
            {
                if (firstVersions.TryGetValue(message.Id, out var version)) Assert.Equal(version, message.RowVersion);
                else firstVersions.Add(message.Id, message.RowVersion);
            }
        }

        Assert.Equal(1, await db.ChatSessions.CountAsync());
        Assert.Equal(3, transport.Calls);
        Assert.Equal(new string?[] { null, "gemini", "openai" }, transport.Providers);
    }

    /// <summary>Both EF providers must keep append operations separate from historical updates.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Append_ShouldTrackOnlyNewMessageAsAdded(bool postgres)
    {
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>();
        if (postgres) options.UseNpgsql("Host=model.invalid;Database=coach_test;Username=coach_test");
        else options.UseSqlite("Data Source=:memory:");
        using var db = new EquilibraFitPlusPlusDbContext(options.Options);
        var session = new ChatSession { TenantId = SeedData.DefaultTenantId, UsuarioId = Guid.NewGuid(), Titulo = "Test" };
        var oldMessage = new ChatMessage { TenantId = session.TenantId, ChatSessionId = session.Id, Role = "user", Conteudo = "Earlier" };
        session.Mensagens.Add(oldMessage);
        db.Attach(session);
        var newMessage = new ChatMessage { TenantId = session.TenantId, ChatSessionId = session.Id, Role = "user", Conteudo = "New" };
        session.Mensagens.Add(newMessage);
        new AiCoachRepository(db).AdicionarMensagem(newMessage);
        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Added, db.Entry(newMessage).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(oldMessage).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(session).State);
        Assert.Same(session, newMessage.ChatSession);
    }

    /// <summary>Owner scoping and true optimistic conflicts must remain enforced.</summary>
    [Fact]
    public async Task Conversation_ShouldRejectOtherOwnerAndDetectConcurrentEdits()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseSqlite(connection).Options;
        await using var first = new EquilibraFitPlusPlusDbContext(options);
        await first.Database.EnsureCreatedAsync();
        var user = new Usuario { TenantId = SeedData.DefaultTenantId, IdentityUserId = Guid.NewGuid(), Nome = "Test", Email = "owner@example.test" };
        var session = new ChatSession { TenantId = user.TenantId, UsuarioId = user.Id, Usuario = user, Titulo = "Test" };
        session.Mensagens.Add(new ChatMessage { TenantId = user.TenantId, Role = "user", Conteudo = "Earlier" });
        first.ChatSessions.Add(session);
        await first.SaveChangesAsync();
        first.ChangeTracker.Clear();
        var repository = new AiCoachRepository(first);
        Assert.Null(await repository.ObterSessaoAsync(user.TenantId, Guid.NewGuid(), session.Id, default));
        Assert.Null(await repository.ObterSessaoAsync(Guid.NewGuid(), user.Id, session.Id, default));
        var loaded = await repository.ObterSessaoAsync(user.TenantId, user.Id, session.Id, default);
        Assert.NotNull(loaded);
        await using var second = new EquilibraFitPlusPlusDbContext(options);
        var stale = await new AiCoachRepository(second).ObterSessaoAsync(user.TenantId, user.Id, session.Id, default);
        loaded.Mensagens.Single().Conteudo = "First edit";
        await first.SaveChangesAsync();
        stale!.Mensagens.Single().Conteudo = "Stale edit";
        var conflict = await Assert.ThrowsAsync<PersistenceConflictException>(() => second.SaveChangesAsync());
        Assert.Equal("persistence.concurrency", conflict.Code);
        first.ChangeTracker.Clear();
        Assert.Equal("First edit", (await first.ChatMessages.SingleAsync()).Conteudo);
    }

    private sealed class EmptyInstructions : IAiInstructionRepository
    {
        public Task<AiInstructionSet?> ObterInstrucoesPublicadasAsync(Guid tenantId, CancellationToken ct)
            => Task.FromResult<AiInstructionSet?>(null);
    }

    private sealed class EmptyContextBuilder : IAiUserContextBuilder
    {
        public Task<CoachAiContext> BuildCoachContextAsync(Guid tenantId, Guid usuarioId, CancellationToken ct)
            => Task.FromResult(new CoachAiContext(1, null, null, null, null, null, null, null, null, [], []));
        public Task<WorkoutAiContext> BuildWorkoutContextAsync(Guid tenantId, Guid usuarioId, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<NutritionAiContext> BuildNutritionContextAsync(Guid tenantId, Guid usuarioId, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<ProgressAiContext> BuildProgressContextAsync(Guid tenantId, Guid usuarioId, CancellationToken ct)
            => throw new NotSupportedException();
    }

    private sealed class CoachTransport(bool fallback) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public List<string?> Providers { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Providers.Add(payload.RootElement.TryGetProperty("provider", out var provider) ? provider.GetString() : null);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"conteudo\":\"Podemos seguir com calma.\",\"modelo\":\"test-model\",\"fallback_used\":{fallback.ToString().ToLowerInvariant()}}}",
                    Encoding.UTF8, "application/json")
            };
        }
    }
}

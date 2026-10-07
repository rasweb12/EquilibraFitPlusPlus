using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.AtualizarRegistroAlimentar;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.UnitTests.Alimentacao;

/// <summary>
/// Tests food log update conflict handling.
/// </summary>
public sealed class AtualizarRegistroAlimentarCommandHandlerTests
{
    /// <summary>
    /// Ensures persistence concurrency is returned as a structured sync conflict.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSyncConflict_WhenPersistenceConflicts()
    {
        Guid tenantId = Guid.NewGuid();
        Guid usuarioId = Guid.NewGuid();
        var registro = new RegistroAlimentar
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            DataHora = DateTimeOffset.UtcNow
        };
        var repository = new FakeAlimentacaoRepository(registro);
        var handler = new AtualizarRegistroAlimentarCommandHandler(
            repository,
            new ConflictingUnitOfWork());
        var request = new AtualizarRegistroAlimentarRequest(
            DateTimeOffset.UtcNow,
            "Almoco",
            [new ItemAlimentarRequest("Arroz", 100, "g", 130, 2.5m, 28, 0.3m, null)]);

        var result = await handler.Handle(
            new AtualizarRegistroAlimentarCommand(
                tenantId,
                usuarioId,
                registro.Id,
                request),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "sync_conflict");
    }

    private sealed class ConflictingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new PersistenceConflictException(
                "persistence.concurrency",
                "Conflito de teste.");
        }
    }

    private sealed class FakeAlimentacaoRepository : IAlimentacaoRepository
    {
        private readonly RegistroAlimentar _registro;

        public FakeAlimentacaoRepository(RegistroAlimentar registro)
        {
            _registro = registro;
        }

        public void AdicionarAnalise(AnaliseRefeicaoImagem analise)
        {
        }

        public void Adicionar(RegistroAlimentar registroAlimentar)
        {
        }

        public void RemoverItens(RegistroAlimentar registroAlimentar)
        {
            registroAlimentar.Itens.Clear();
        }

        public Task<RegistroAlimentar?> ObterPorIdAsync(
            Guid tenantId,
            Guid usuarioId,
            Guid registroId,
            CancellationToken cancellationToken)
        {
            RegistroAlimentar? result =
                _registro.TenantId == tenantId &&
                _registro.UsuarioId == usuarioId &&
                _registro.Id == registroId
                    ? _registro
                    : null;

            return Task.FromResult(result);
        }

        public Task<PagedResult<RegistroAlimentar>> ListarAsync(
            Guid tenantId,
            Guid usuarioId,
            DateOnly? inicio,
            DateOnly? fim,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new PagedResult<RegistroAlimentar>([], page, pageSize, 0));
        }
    }
}

namespace Planner.Application.Common;

/// <summary>Grava, numa transação só, tudo o que o caso de uso mudou.</summary>
public interface IUnitOfWork {
    /// <exception cref="UseCaseException">
    /// Conflito com o estado do banco: versão desatualizada (If-Match) ou violação de unicidade.
    /// </exception>
    Task SaveChangesAsync(CancellationToken ct);
}

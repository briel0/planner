using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planner.Application.Common;

namespace Planner.Infrastructure.Persistence;

/// <summary>Salva as mudanças e traduz os conflitos do banco em erros dos casos de uso.</summary>
internal sealed class UnitOfWork(PlannerDbContext db) : IUnitOfWork {
    /// <summary>Restrições de unicidade e o erro que cada uma representa (cobre corridas entre dois pedidos).</summary>
    private static readonly Dictionary<string, (string Code, string Message)> UniqueViolations = new() {
        ["ux_categories_user_id_lower_name"] = ("category.name-taken", "A category with this name already exists."),
        ["pk_categories"] = ("id-taken", "This id is already in use."),
    };

    public async Task SaveChangesAsync(CancellationToken ct) {
        try {
            await db.SaveChangesAsync(ct);
        } catch(DbUpdateConcurrencyException) {
            throw UseCaseException.StaleVersion();
        } catch(DbUpdateException error) when(error.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: { } constraint,
        } && UniqueViolations.ContainsKey(constraint)) {
            var (code, message) = UniqueViolations[constraint];
            throw new UseCaseException(UseCaseErrorKind.Conflict, code, message);
        }
    }
}

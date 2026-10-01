using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Categories;
using Planner.Infrastructure.Persistence.Configurations;

namespace Planner.Infrastructure.Persistence.Categories;

internal sealed class CategoryQueries(PlannerDbContext db) : ICategoryQueries {
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(Guid userId, CancellationToken ct) =>
        (await Project(db.Categories.Where(c => c.UserId == userId).OrderBy(c => c.SortOrder)).ToListAsync(ct))
        .ConvertAll(ToResponse);

    public async Task<CategoryResponse?> FindAsync(Guid userId, Guid id, CancellationToken ct) {
        var row = await Project(db.Categories.Where(c => c.UserId == userId && c.Id == id)).SingleOrDefaultAsync(ct);
        return row is null ? null : ToResponse(row);
    }

    /// <summary>Só as colunas da resposta, sem rastrear entidades; datas e versão vêm das colunas ocultas.</summary>
    private static IQueryable<Row> Project(IQueryable<Domain.Categories.Category> categories) =>
        categories.AsNoTracking().Select(c => new Row(
            c.Id,
            c.Name,
            c.SortOrder,
            EF.Property<DateTimeOffset>(c, "CreatedAt"),
            EF.Property<DateTimeOffset>(c, "UpdatedAt"),
            EF.Property<uint>(c, Timestamps.VersionProperty)));

    private static CategoryResponse ToResponse(Row row) => new(
        row.Id, row.Name, row.SortOrder, row.CreatedAt, row.UpdatedAt, row.Version.ToString(CultureInfo.InvariantCulture));

    private sealed record Row(
        Guid Id, string Name, int SortOrder, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, uint Version);
}

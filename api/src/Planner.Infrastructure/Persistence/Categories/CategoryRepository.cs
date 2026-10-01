using Microsoft.EntityFrameworkCore;
using Planner.Application.Categories;
using Planner.Application.Common;
using Planner.Domain.Categories;
using Planner.Infrastructure.Persistence.Configurations;

namespace Planner.Infrastructure.Persistence.Categories;

internal sealed class CategoryRepository(PlannerDbContext db) : ICategoryRepository {
    public Task<Category?> FindAsync(Guid userId, Guid id, CancellationToken ct) =>
        db.Categories.SingleOrDefaultAsync(c => c.UserId == userId && c.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => db.Categories.AnyAsync(c => c.Id == id, ct);

    // Esta expressão não roda em C#: o EF Core a traduz para lower(name) = lower(@name) no SQL, a mesma expressão do
    // índice único (user_id, lower(name)). As regras de cultura do .NET não se aplicam aqui.
#pragma warning disable CA1304, CA1311, CA1862
    public Task<bool> NameTakenAsync(Guid userId, string name, Guid ignoreId, CancellationToken ct) =>
        db.Categories.AnyAsync(
            c => c.UserId == userId && c.Id != ignoreId && c.Name.ToLower() == name.ToLower(), ct);
#pragma warning restore CA1304, CA1311, CA1862

    public async Task<int> NextSortOrderAsync(Guid userId, CancellationToken ct) =>
        (await db.Categories.Where(c => c.UserId == userId).MaxAsync(c => (int?)c.SortOrder, ct) ?? -1) + 1;

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);

    public void ExpectVersion(Category category, string version) {
        if(!uint.TryParse(version, out var expected)) {
            throw UseCaseException.StaleVersion();
        }
        db.Entry(category).Property<uint>(Timestamps.VersionProperty).OriginalValue = expected;
    }
}

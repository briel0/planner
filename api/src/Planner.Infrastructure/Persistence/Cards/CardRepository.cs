using Microsoft.EntityFrameworkCore;
using Planner.Application.Cards;
using Planner.Application.Common;
using Planner.Domain.Cards;
using Planner.Infrastructure.Persistence.Configurations;

namespace Planner.Infrastructure.Persistence.Cards;

internal sealed class CardRepository(PlannerDbContext db) : ICardRepository {
    public Task<Card?> FindAsync(Guid userId, Guid id, CancellationToken ct) =>
        db.Cards.SingleOrDefaultAsync(
            c => c.Id == id && db.Categories.Any(category => category.Id == c.CategoryId && category.UserId == userId), ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => db.Cards.AnyAsync(c => c.Id == id, ct);

    public async Task<int> NextLayerAsync(Guid categoryId, Guid? parentId, CancellationToken ct) =>
        (await db.Cards
            .Where(c => c.CategoryId == categoryId && c.ParentId == parentId)
            .MaxAsync(c => (int?)c.Layer, ct) ?? -1) + 1;

    public void Add(Card card) => db.Cards.Add(card);

    public void Remove(Card card) => db.Cards.Remove(card);

    public void ExpectVersion(Card card, string version) {
        if(!uint.TryParse(version, out var expected)) {
            throw UseCaseException.StaleVersion();
        }
        db.Entry(card).Property<uint>(Timestamps.VersionProperty).OriginalValue = expected;
    }
}

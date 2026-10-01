using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Planner.Application.Cards;
using Planner.Domain.Cards;
using Planner.Infrastructure.Persistence.Configurations;

namespace Planner.Infrastructure.Persistence.Cards;

internal sealed class CardQueries(PlannerDbContext db) : ICardQueries {
    public async Task<IReadOnlyList<CardResponse>?> ListRootAsync(Guid userId, Guid categoryId, CancellationToken ct) {
        if(!await db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, ct)) {
            return null;
        }
        return (await Project(db.Cards.Where(c => c.CategoryId == categoryId && c.ParentId == null)).ToListAsync(ct))
            .ConvertAll(row => ToResponse(row));
    }

    public async Task<IReadOnlyList<CardResponse>?> ListChildrenAsync(Guid userId, Guid cardId, CancellationToken ct) {
        if(!await OwnedBy(userId).AnyAsync(c => c.Id == cardId, ct)) {
            return null;
        }
        return (await Project(db.Cards.Where(c => c.ParentId == cardId)).ToListAsync(ct)).ConvertAll(row => ToResponse(row));
    }

    public async Task<CardResponse?> FindAsync(Guid userId, Guid id, CancellationToken ct) {
        var row = await Project(OwnedBy(userId).Where(c => c.Id == id)).SingleOrDefaultAsync(ct);
        return row is null ? null : ToResponse(row, await AncestorsAsync(id, ct));
    }

    private IQueryable<Card> OwnedBy(Guid userId) =>
        db.Cards.Where(c => db.Categories.Any(category => category.Id == c.CategoryId && category.UserId == userId));

    /// <summary>
    /// Os cartões acima deste, da raiz até o pai direto: sobe a árvore com uma consulta recursiva (a mesma técnica do
    /// trigger que impede ciclos), numa ida só ao banco.
    /// </summary>
    private async Task<IReadOnlyList<CardRef>> AncestorsAsync(Guid id, CancellationToken ct) {
        var rows = await db.Database.SqlQuery<AncestorRow>($"""
            WITH RECURSIVE ancestors AS (
                SELECT p.id, p.parent_id, p.title, 1 AS depth
                FROM cards c JOIN cards p ON p.id = c.parent_id
                WHERE c.id = {id}
                UNION ALL
                SELECT p.id, p.parent_id, p.title, a.depth + 1
                FROM cards p JOIN ancestors a ON p.id = a.parent_id
            )
            SELECT id, title, depth FROM ancestors
            """).ToListAsync(ct);
        return [.. rows.OrderByDescending(r => r.Depth).Select(r => new CardRef(r.Id, r.Title))];
    }

    /// <summary>Só as colunas da resposta, sem rastrear entidades; datas e versão vêm das colunas ocultas.</summary>
    private IQueryable<Row> Project(IQueryable<Card> cards) =>
        cards.AsNoTracking().Select(c => new Row(
            c.Id,
            c.CategoryId,
            c.ParentId,
            c.Title,
            c.Color,
            c.Position.X,
            c.Position.Y,
            c.Size.Width,
            c.Size.Height,
            c.Layer,
            c.Properties,
            c.Content,
            db.Cards.Count(child => child.ParentId == c.Id),
            EF.Property<DateTimeOffset>(c, "CreatedAt"),
            EF.Property<DateTimeOffset>(c, "UpdatedAt"),
            EF.Property<uint>(c, Timestamps.VersionProperty)));

    private static CardResponse ToResponse(Row row, IReadOnlyList<CardRef>? ancestors = null) => new(
        row.Id,
        row.CategoryId,
        row.ParentId,
        row.Title,
        row.Color,
        new PositionDto(row.X, row.Y),
        new SizeDto(row.Width, row.Height),
        row.Layer,
        new CardPropertiesDto { DueOn = row.Properties.DueOn, Done = row.Properties.Done },
        row.Content is null ? null : ParseContent(row.Content),
        row.ChildCount,
        row.CreatedAt,
        row.UpdatedAt,
        row.Version.ToString(CultureInfo.InvariantCulture),
        ancestors);

    private static JsonElement ParseContent(string json) {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed record Row(
        Guid Id,
        Guid CategoryId,
        Guid? ParentId,
        string Title,
        string Color,
        double X,
        double Y,
        double Width,
        double Height,
        int Layer,
        CardProperties Properties,
        string? Content,
        int ChildCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint Version);

    private sealed record AncestorRow(Guid Id, string Title, int Depth);
}

using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planner.Domain.Cards;
using Planner.Domain.Categories;
using Planner.Domain.Users;
using Planner.Infrastructure.Persistence;

namespace Planner.IntegrationTests.Persistence;

/// <summary>Regras que só o banco garante (docs/data-modeling/04-physical-model.md), testadas num Postgres real.</summary>
[Collection(nameof(PlannerTestGroup))]
public class DatabaseRulesTests(PlannerFixture postgres) {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Card_round_trips_with_value_objects_and_properties() {
        var category = await CreateCategoryAsync();
        var card = Card.Create(Guid.CreateVersion7(), category.Id, "Lista 3");
        card.ChangeColor("#3B82F6");
        card.MoveTo(new Position(120.5, -80));
        card.Resize(new Size(300.5, 120));
        card.ChangeContent("""{"type": "doc", "content": []}""");
        card.ChangeProperties(new CardProperties(DueOn: new DateOnly(2026, 9, 30)));
        await SaveAsync(card);

        await using var db = postgres.NewContext();
        var loaded = await db.Cards.SingleAsync(c => c.Id == card.Id, Ct);
        Assert.Equal("#3b82f6", loaded.Color);
        Assert.Equal(new Position(120.5, -80), loaded.Position);
        Assert.Equal(new Size(300.5, 120), loaded.Size);
        Assert.Equal(new CardProperties(DueOn: new DateOnly(2026, 9, 30)), loaded.Properties);
        Assert.NotNull(loaded.Content);

        // Propriedades ausentes não são gravadas, e as chaves seguem o catálogo em snake_case.
        var json = await db.Database
            .SqlQuery<string>($"SELECT properties::text AS \"Value\" FROM cards WHERE id = {card.Id}")
            .SingleAsync(Ct);
        Assert.Equal("""{"due_on": "2026-09-30"}""", json);
    }

    [Fact]
    public async Task Category_names_are_unique_per_user_ignoring_letter_case() {
        var first = await CreateCategoryAsync("Faculdade");
        var duplicate = Category.Create(Guid.CreateVersion7(), first.UserId, "faculdade", sortOrder: 1);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(duplicate));

        AssertViolation(error, PostgresErrorCodes.UniqueViolation, "ux_categories_user_id_lower_name");
    }

    [Fact]
    public async Task Child_card_cannot_have_a_category_different_from_its_parent() {
        var category = await CreateCategoryAsync();
        var otherCategory = await CreateCategoryAsync();
        var parent = Card.Create(Guid.CreateVersion7(), category.Id, "Física Quântica");
        var child = Card.CreateInside(Guid.CreateVersion7(), parent, "Lista 3");
        await SaveAsync(parent, child);

        // O domínio nunca faz isso; aqui o banco é testado diretamente, como última linha de defesa.
        await using var db = postgres.NewContext();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE cards SET category_id = {otherCategory.Id} WHERE id = {child.Id}", Ct));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
        Assert.Equal("fk_cards_parent_same_category", error.ConstraintName);
    }

    [Fact]
    public async Task Moving_a_root_card_to_another_category_moves_its_whole_subtree() {
        var category = await CreateCategoryAsync();
        var otherCategory = await CreateCategoryAsync();
        var root = Card.Create(Guid.CreateVersion7(), category.Id, "Física Quântica");
        var child = Card.CreateInside(Guid.CreateVersion7(), root, "Lista 3");
        var grandchild = Card.CreateInside(Guid.CreateVersion7(), child, "Exercício 5");
        await SaveAsync(root, child, grandchild);

        await using(var db = postgres.NewContext()) {
            var loaded = await db.Cards.SingleAsync(c => c.Id == root.Id, Ct);
            loaded.MoveToRoot(otherCategory.Id);
            await db.SaveChangesAsync(Ct);
        }

        await using var check = postgres.NewContext();
        var categories = await check.Cards
            .Where(c => c.Id == child.Id || c.Id == grandchild.Id)
            .Select(c => c.CategoryId)
            .ToListAsync(Ct);
        Assert.All(categories, id => Assert.Equal(otherCategory.Id, id));
    }

    [Fact]
    public async Task Card_cannot_be_moved_inside_one_of_its_descendants() {
        var category = await CreateCategoryAsync();
        var a = Card.Create(Guid.CreateVersion7(), category.Id, "A");
        var b = Card.CreateInside(Guid.CreateVersion7(), a, "B");
        var c = Card.CreateInside(Guid.CreateVersion7(), b, "C");
        await SaveAsync(a, b, c);

        await using var db = postgres.NewContext();
        var loadedA = await db.Cards.SingleAsync(card => card.Id == a.Id, Ct);
        var loadedC = await db.Cards.SingleAsync(card => card.Id == c.Id, Ct);
        loadedA.MoveInto(loadedC); // permitido pelo domínio, que só enxerga o pai direto

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));

        AssertViolation(error, PostgresErrorCodes.CheckViolation, "ck_cards_no_cycle");
    }

    [Fact]
    public async Task Deleting_a_category_deletes_all_of_its_cards() {
        var category = await CreateCategoryAsync();
        var root = Card.Create(Guid.CreateVersion7(), category.Id, "Física Quântica");
        var child = Card.CreateInside(Guid.CreateVersion7(), root, "Lista 3");
        await SaveAsync(root, child);

        await using(var db = postgres.NewContext()) {
            await db.Categories.Where(c => c.Id == category.Id).ExecuteDeleteAsync(Ct);
        }

        await using var check = postgres.NewContext();
        Assert.False(await check.Cards.AnyAsync(c => c.CategoryId == category.Id, Ct));
    }

    [Fact]
    public async Task Updated_at_is_refreshed_on_every_update() {
        var category = await CreateCategoryAsync();
        var card = Card.Create(Guid.CreateVersion7(), category.Id, "Lista 3");
        await SaveAsync(card);
        var before = await UpdatedAtAsync(card.Id);

        await using(var db = postgres.NewContext()) {
            var loaded = await db.Cards.SingleAsync(c => c.Id == card.Id, Ct);
            loaded.Rename("Lista 4");
            await db.SaveChangesAsync(Ct);
        }

        Assert.True(await UpdatedAtAsync(card.Id) > before);
    }

    [Fact]
    public async Task Invalid_color_is_rejected_by_the_database() {
        var category = await CreateCategoryAsync();
        var card = Card.Create(Guid.CreateVersion7(), category.Id, "Lista 3");
        await SaveAsync(card);

        await using var db = postgres.NewContext();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE cards SET color = {"#FFFFFF"} WHERE id = {card.Id}", Ct));

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("ck_cards_color", error.ConstraintName);
    }

    [Fact]
    public async Task Size_outside_the_limits_is_rejected_by_the_database() {
        var category = await CreateCategoryAsync();
        var card = Card.Create(Guid.CreateVersion7(), category.Id, "Lista 3");
        await SaveAsync(card);

        await using var db = postgres.NewContext();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE cards SET width = {50} WHERE id = {card.Id}", Ct));

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("ck_cards_size", error.ConstraintName);
    }

    private async Task<Category> CreateCategoryAsync(string name = "Faculdade") {
        var user = User.Create("Gabriel", new Email($"{Guid.NewGuid():N}@example.com"));
        var category = Category.Create(Guid.CreateVersion7(), user.Id, name, sortOrder: 0);
        await using var db = postgres.NewContext();
        db.Users.Add(user);
        db.Categories.Add(category);
        await db.SaveChangesAsync(Ct);
        return category;
    }

    private async Task SaveAsync(params object[] entities) {
        await using var db = postgres.NewContext();
        db.AddRange(entities);
        await db.SaveChangesAsync(Ct);
    }

    private async Task<DateTimeOffset> UpdatedAtAsync(Guid cardId) {
        await using var db = postgres.NewContext();
        return await db.Cards
            .Where(c => c.Id == cardId)
            .Select(c => EF.Property<DateTimeOffset>(c, "UpdatedAt"))
            .SingleAsync(Ct);
    }

    private static void AssertViolation(DbUpdateException error, string sqlState, string constraint) {
        var postgresError = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(sqlState, postgresError.SqlState);
        Assert.Equal(constraint, postgresError.ConstraintName);
    }
}

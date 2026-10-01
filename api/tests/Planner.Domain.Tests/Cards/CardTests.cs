using Planner.Domain.Cards;

namespace Planner.Domain.Tests.Cards;

public class CardTests {
    private static Card CreateCard() => Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), "Lista 3");

    [Fact]
    public void ChangeColor_normalizes_to_lowercase() {
        var card = CreateCard();

        card.ChangeColor("#3B82F6");

        Assert.Equal("#3b82f6", card.Color);
    }

    [Theory]
    [InlineData("azul")]
    [InlineData("#3b82f")]
    [InlineData("#3b82f6ff")]
    [InlineData("3b82f6")]
    [InlineData("#gggggg")]
    public void ChangeColor_rejects_invalid_format(string color) {
        var card = CreateCard();

        var error = Assert.Throws<DomainException>(() => card.ChangeColor(color));

        Assert.Equal("card.invalid-color", error.Code);
    }

    [Fact]
    public void Create_trims_title() {
        var card = Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), "  Lista 3  ");

        Assert.Equal("Lista 3", card.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_title(string title) {
        var error = Assert.Throws<DomainException>(() => Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), title));

        Assert.Equal("card.invalid-title", error.Code);
    }

    [Fact]
    public void Create_accepts_title_with_200_characters() {
        var title = new string('a', 200);

        var card = Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), title);

        Assert.Equal(title, card.Title);
    }

    [Fact]
    public void Create_rejects_title_longer_than_200_characters() {
        var title = new string('a', 201);

        var error = Assert.Throws<DomainException>(() => Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), title));

        Assert.Equal("card.invalid-title", error.Code);
    }

    [Fact]
    public void Create_rejects_ids_that_are_not_uuid_version_7() {
        var error = Assert.Throws<DomainException>(
            () => Card.Create(Guid.NewGuid(), categoryId: Guid.CreateVersion7(), "Lista 3"));

        Assert.Equal("invalid-id", error.Code);
    }

    [Fact]
    public void Rename_applies_the_same_title_rules() {
        var card = CreateCard();

        card.Rename("  Prova 1  ");

        Assert.Equal("Prova 1", card.Title);
        var error = Assert.Throws<DomainException>(() => card.Rename("   "));
        Assert.Equal("card.invalid-title", error.Code);
    }

    [Fact]
    public void ChangeContent_accepts_a_json_object_or_null() {
        var card = CreateCard();

        card.ChangeContent("""{"type":"doc","content":[]}""");
        Assert.Equal("""{"type":"doc","content":[]}""", card.Content);

        card.ChangeContent(null);
        Assert.Null(card.Content);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("\"text\"")]
    [InlineData("not json")]
    [InlineData("{\"unclosed\": ")]
    public void ChangeContent_rejects_anything_but_a_json_object(string content) {
        var card = CreateCard();

        var error = Assert.Throws<DomainException>(() => card.ChangeContent(content));

        Assert.Equal("card.invalid-content", error.Code);
    }

    [Fact]
    public void CreateInside_places_the_card_in_the_parent_board_and_category() {
        var parent = CreateCard();

        var child = Card.CreateInside(Guid.CreateVersion7(), parent, "Exercício 5");

        Assert.Equal(parent.Id, child.ParentId);
        Assert.Equal(parent.CategoryId, child.CategoryId);
    }

    [Fact]
    public void MoveInto_takes_the_parent_category() {
        var parent = Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), "Física Quântica");
        var card = Card.Create(Guid.CreateVersion7(), categoryId: Guid.CreateVersion7(), "Lista 3");

        card.MoveInto(parent);

        Assert.Equal(parent.Id, card.ParentId);
        Assert.Equal(parent.CategoryId, card.CategoryId);
    }

    [Fact]
    public void MoveInto_rejects_moving_a_card_inside_itself() {
        var card = CreateCard();

        var error = Assert.Throws<DomainException>(() => card.MoveInto(card));

        Assert.Equal("card.cycle", error.Code);
    }

    [Fact]
    public void MoveToRoot_leaves_the_parent_board() {
        var parent = CreateCard();
        var card = Card.CreateInside(Guid.CreateVersion7(), parent, "Lista 3");
        var otherCategory = Guid.CreateVersion7();

        card.MoveToRoot(otherCategory);

        Assert.Null(card.ParentId);
        Assert.Equal(otherCategory, card.CategoryId);
    }
}

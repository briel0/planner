using Planner.Domain.Cards;

namespace Planner.Domain.Tests.Cards;

public class CardTests {
    private static Card CreateCard() => Card.Create(Guid.CreateVersion7(), "Lista 3");

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
}

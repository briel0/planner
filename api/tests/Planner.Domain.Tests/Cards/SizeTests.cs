using Planner.Domain.Cards;

namespace Planner.Domain.Tests.Cards;

public class SizeTests {
    [Fact]
    public void Accepts_the_minimum_and_maximum_sizes() {
        Assert.Equal(new Size(120, 48), new Size(Size.Min.Width, Size.Min.Height));
        Assert.Equal(new Size(1200, 900), new Size(Size.Max.Width, Size.Max.Height));
    }

    [Theory]
    [InlineData(119.9, 64)]
    [InlineData(1200.1, 64)]
    [InlineData(208, 47.9)]
    [InlineData(208, 900.1)]
    [InlineData(double.NaN, 64)]
    [InlineData(208, double.PositiveInfinity)]
    public void Rejects_sizes_outside_the_limits(double width, double height) {
        var error = Assert.Throws<DomainException>(() => new Size(width, height));

        Assert.Equal("card.invalid-size", error.Code);
    }

    [Fact]
    public void New_cards_have_the_default_size_and_can_be_resized() {
        var card = Card.Create(Guid.CreateVersion7(), "Lista 3");
        Assert.Equal(new Size(208, 64), card.Size);

        card.Resize(new Size(300, 120));

        Assert.Equal(new Size(300, 120), card.Size);
    }
}

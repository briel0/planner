using Planner.Domain.Cards;

namespace Planner.Domain.Tests.Cards;

public class PositionTests {
    [Fact]
    public void Accepts_finite_coordinates_including_fractions_and_negatives() {
        var position = new Position(-120.5, 80.25);

        Assert.Equal(-120.5, position.X);
        Assert.Equal(80.25, position.Y);
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(0, double.NegativeInfinity)]
    public void Rejects_non_finite_coordinates(double x, double y) {
        var error = Assert.Throws<DomainException>(() => new Position(x, y));

        Assert.Equal("card.invalid-position", error.Code);
    }
}

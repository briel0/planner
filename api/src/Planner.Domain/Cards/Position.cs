namespace Planner.Domain.Cards;

/// <summary>Posição de um cartão no canvas. As coordenadas são sempre números finitos.</summary>
public readonly record struct Position {
    public Position(double x, double y) {
        if(!double.IsFinite(x) || !double.IsFinite(y)) {
            throw new DomainException("card.invalid-position", "Card coordinates must be finite numbers.");
        }
        X = x;
        Y = y;
    }

    public static Position Origin { get; } = new(0, 0);

    public double X { get; }

    public double Y { get; }
}

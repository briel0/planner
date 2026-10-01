namespace Planner.Domain.Cards;

/// <summary>
/// Tamanho de um cartão no canvas, entre <see cref="Min"/> e <see cref="Max"/>. É um record de classe (e não
/// struct, como a <see cref="Position"/>) porque o valor "vazio" de um struct (0 × 0) seria um tamanho inválido
/// criado sem passar pela validação.
/// </summary>
public sealed record Size {
    public const double MinWidth = 120;
    public const double MinHeight = 48;
    public const double MaxWidth = 1200;
    public const double MaxHeight = 900;

    public static Size Min { get; } = new(MinWidth, MinHeight);

    public static Size Max { get; } = new(MaxWidth, MaxHeight);

    /// <summary>O tamanho dos cartões novos (e o que os cartões tinham antes de poderem ser redimensionados).</summary>
    public static Size Default { get; } = new(208, 64);

    public Size(double width, double height) {
        // A forma negada também rejeita NaN (que falha em qualquer comparação).
        if(!(width is >= MinWidth and <= MaxWidth) || !(height is >= MinHeight and <= MaxHeight)) {
            throw new DomainException(
                "card.invalid-size",
                $"A card must be between {MinWidth} × {MinHeight} and {MaxWidth} × {MaxHeight} pixels.");
        }
        Width = width;
        Height = height;
    }

    public double Width { get; }

    public double Height { get; }
}

using System.Text.RegularExpressions;

namespace Planner.Domain.Cards;

public sealed partial class Card {
    public const string DefaultColor = "#e5e7eb";

    private Card(Guid id, Guid categoryId, string title) {
        Id = id;
        CategoryId = categoryId;
        Title = title;
        Color = DefaultColor;
    }

    public Guid Id { get; }

    public Guid CategoryId { get; private set; }

    public string Title { get; private set; }

    /// <summary>Cor livre do cartão, sempre no formato <c>#rrggbb</c> em minúsculas.</summary>
    public string Color { get; private set; }

    public static Card Create(Guid categoryId, string title) => new(Guid.CreateVersion7(), categoryId, title);

    public void ChangeColor(string color) {
        if(!HexColorPattern().IsMatch(color)) {
            throw new DomainException("card.invalid-color", $"'{color}' is not a color in the #rrggbb format.");
        }
        Color = color.ToLowerInvariant();
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorPattern();
}

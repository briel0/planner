using System.Text.RegularExpressions;

namespace Planner.Domain.Cards;

public sealed partial class Card {
    public const string DefaultColor = "#e5e7eb";
    public const int TitleMaxLength = 200;

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

    public static Card Create(Guid categoryId, string title) =>
        new(Guid.CreateVersion7(), categoryId, NormalizeTitle(title));

    public void ChangeColor(string color) {
        if(!HexColorPattern().IsMatch(color)) {
            throw new DomainException("card.invalid-color", $"'{color}' is not a color in the #rrggbb format.");
        }
        Color = color.ToLowerInvariant();
    }

    private static string NormalizeTitle(string title) {
        var trimmed = title.Trim();
        if(trimmed.Length == 0 || trimmed.Length > TitleMaxLength) {
            throw new DomainException(
                "card.invalid-title", $"A card title must have between 1 and {TitleMaxLength} characters.");
        }
        return trimmed;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorPattern();
}

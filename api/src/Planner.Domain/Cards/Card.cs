using System.Text.Json;
using System.Text.RegularExpressions;

namespace Planner.Domain.Cards;

/// <summary>
/// Um cartão num quadro. Pode estar na raiz do quadro de uma categoria ou dentro do quadro de outro cartão
/// (ver docs/data-modeling/).
/// </summary>
public sealed partial class Card {
    public const string DefaultColor = "#e5e7eb";
    public const int TitleMaxLength = 200;

    private Card(Guid id, Guid categoryId, Guid? parentId, string title) {
        Id = id;
        CategoryId = categoryId;
        ParentId = parentId;
        Title = title;
        Color = DefaultColor;
        Position = Position.Origin;
        Properties = CardProperties.None;
    }

    public Guid Id { get; }

    /// <summary>Categoria do cartão, em qualquer nível da árvore. Sempre igual à do pai.</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Cartão que contém este; nulo quando o cartão está na raiz do quadro da categoria.</summary>
    public Guid? ParentId { get; private set; }

    public string Title { get; private set; }

    /// <summary>Cor livre do cartão, sempre no formato <c>#rrggbb</c> em minúsculas.</summary>
    public string Color { get; private set; }

    public Position Position { get; private set; }

    /// <summary>Ordem de sobreposição no quadro: maior fica na frente.</summary>
    public int Layer { get; private set; }

    /// <summary>Documento do Tiptap em JSON (sempre um objeto), ou nulo quando o cartão não tem conteúdo.</summary>
    public string? Content { get; private set; }

    public CardProperties Properties { get; private set; }

    /// <summary>Cria um cartão na raiz do quadro de uma categoria.</summary>
    public static Card Create(Guid categoryId, string title) =>
        new(Guid.CreateVersion7(), categoryId, parentId: null, NormalizeTitle(title));

    /// <summary>Cria um cartão dentro do quadro de outro cartão, na mesma categoria dele.</summary>
    public static Card CreateInside(Card parent, string title) {
        ArgumentNullException.ThrowIfNull(parent);
        return new(Guid.CreateVersion7(), parent.CategoryId, parent.Id, NormalizeTitle(title));
    }

    public void Rename(string title) => Title = NormalizeTitle(title);

    public void ChangeColor(string color) {
        if(!HexColorPattern().IsMatch(color)) {
            throw new DomainException("card.invalid-color", $"'{color}' is not a color in the #rrggbb format.");
        }
        Color = color.ToLowerInvariant();
    }

    public void MoveTo(Position position) => Position = position;

    public void ChangeLayer(int layer) => Layer = layer;

    public void ChangeContent(string? content) {
        if(content is not null && !IsJsonObject(content)) {
            throw new DomainException("card.invalid-content", "Card content must be a JSON object.");
        }
        Content = content;
    }

    public void ChangeProperties(CardProperties properties) {
        ArgumentNullException.ThrowIfNull(properties);
        Properties = properties;
    }

    /// <summary>
    /// Move o cartão para dentro do quadro de <paramref name="parent"/>, assumindo a categoria dele.
    /// Aqui só se verifica o ciclo direto (o cartão dentro de si mesmo); ciclos mais profundos exigem a
    /// árvore inteira e são barrados pelo trigger do banco.
    /// </summary>
    public void MoveInto(Card parent) {
        ArgumentNullException.ThrowIfNull(parent);
        if(parent.Id == Id) {
            throw new DomainException("card.cycle", "A card cannot be moved inside itself.");
        }
        ParentId = parent.Id;
        CategoryId = parent.CategoryId;
    }

    /// <summary>Move o cartão para a raiz do quadro de uma categoria (a mesma ou outra).</summary>
    public void MoveToRoot(Guid categoryId) {
        ParentId = null;
        CategoryId = categoryId;
    }

    private static string NormalizeTitle(string title) =>
        RequiredText.Normalize(title, TitleMaxLength, "card.invalid-title", "A card title");

    private static bool IsJsonObject(string json) {
        try {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        } catch(JsonException) {
            return false;
        }
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorPattern();
}

using Planner.Domain;
using Planner.Domain.Cards;

namespace Planner.Application.Cards;

/// <summary>Onde um cartão fica: na raiz do quadro de uma categoria, ou dentro de outro cartão (exatamente um).</summary>
public sealed record CardLocation(Guid? CategoryId, Guid? ParentId) {
    public static CardLocation Validate(Guid? categoryId, Guid? parentId) =>
        (categoryId is null) == (parentId is null)
            ? throw new DomainException(
                "card.invalid-location", "Give exactly one of categoryId (board root) or parentId (inside a card).")
            : new(categoryId, parentId);
}

/// <summary>A nova descrição: <c>Json</c> nulo apaga a descrição. (Ausente do PATCH = sem mudança.)</summary>
public sealed record ContentChange(string? Json);

/// <summary>As alterações de um PATCH; só as não nulas são aplicadas.</summary>
public sealed record CardChanges(
    string? Title = null,
    string? Color = null,
    Position? Position = null,
    Size? Size = null,
    ContentChange? Content = null,
    CardProperties? Properties = null,
    CardLocation? Location = null);

using System.Text.Json;
using Planner.Application.Cards;

namespace Planner.Api.Cards;

/// <param name="Id">Gerado pelo cliente (UUID v7): repetir o pedido com o mesmo id não duplica o cartão.</param>
/// <param name="Title">Título: 1 a 200 caracteres.</param>
/// <param name="Position">Onde o cartão aparece no quadro.</param>
public sealed record CreateCardRequest(Guid Id, string Title, PositionDto Position);

/// <summary>Para onde mover: exatamente um dos dois.</summary>
public sealed record CardLocationRequest {
    /// <summary>Para a raiz do quadro desta categoria.</summary>
    public Guid? CategoryId { get; init; }

    /// <summary>Para dentro do quadro deste cartão (assumindo a categoria dele).</summary>
    public Guid? ParentId { get; init; }
}

/// <summary>Só os campos enviados são alterados (todos opcionais).</summary>
public sealed record UpdateCardRequest {
    public string? Title { get; init; }

    /// <summary>Cor livre, no formato #rrggbb.</summary>
    public string? Color { get; init; }

    public PositionDto? Position { get; init; }

    public SizeDto? Size { get; init; }

    /// <summary>
    /// A descrição (documento do Tiptap, sempre um objeto JSON). <c>null</c> apaga a descrição; ausente, não muda.
    /// </summary>
    /// <remarks>
    /// Não é <c>JsonElement?</c> de propósito: com o <c>?</c>, o JSON <c>null</c> e o campo ausente virariam o mesmo
    /// <c>null</c> do C#. Sem ele, ausente é <see cref="JsonValueKind.Undefined"/> e <c>null</c> é
    /// <see cref="JsonValueKind.Null"/>.
    /// </remarks>
    public JsonElement Content { get; init; }

    /// <summary>Substitui todas as propriedades do catálogo (as ausentes ou nulas são removidas).</summary>
    public CardPropertiesDto? Properties { get; init; }

    /// <summary>Mover o cartão para outro quadro.</summary>
    public CardLocationRequest? Location { get; init; }
}

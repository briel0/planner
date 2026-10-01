using System.Text.Json;
using System.Text.Json.Serialization;

namespace Planner.Application.Cards;

public sealed record PositionDto(double X, double Y);

public sealed record SizeDto(double Width, double Height);

/// <summary>Propriedades do catálogo; cada uma é opcional (ausente ou nula quando o cartão não a tem).</summary>
public sealed record CardPropertiesDto {
    /// <summary>Prazo: o dia até o qual o cartão precisa estar pronto.</summary>
    public DateOnly? DueOn { get; init; }

    /// <summary>Se o cartão está concluído.</summary>
    public bool? Done { get; init; }
}

/// <summary>Referência curta a um cartão, para a trilha de navegação.</summary>
public sealed record CardRef(Guid Id, string Title);

/// <summary>Um cartão como a API o devolve (docs/api-design.md).</summary>
/// <param name="ParentId">Cartão que contém este; nulo na raiz do quadro da categoria.</param>
/// <param name="Content">Descrição em texto rico (documento do Tiptap); nula quando não há descrição.</param>
/// <param name="ChildCount">Quantos cartões existem no quadro deste.</param>
/// <param name="Version">Versão do cartão, para o If-Match (concorrência otimista).</param>
/// <param name="Ancestors">
/// Só na representação completa (GET /cards/{id}): os cartões acima deste, da raiz até o pai direto.
/// </param>
public sealed record CardResponse(
    Guid Id,
    Guid CategoryId,
    Guid? ParentId,
    string Title,
    string Color,
    PositionDto Position,
    SizeDto Size,
    int Layer,
    CardPropertiesDto Properties,
    JsonElement? Content,
    int ChildCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CardRef>? Ancestors = null);

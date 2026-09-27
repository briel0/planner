namespace Planner.Domain.Cards;

/// <summary>
/// Propriedades do catálogo do sistema (ver docs/data-modeling/03-logical-model.md). Cada propriedade é
/// opcional: nula quando o cartão não a tem. Uma propriedade nova no catálogo é um parâmetro novo aqui.
/// </summary>
/// <param name="DueOn">Prazo: o dia até o qual o cartão precisa estar pronto.</param>
/// <param name="Done">Se o cartão está concluído.</param>
public sealed record CardProperties(DateOnly? DueOn = null, bool? Done = null) {
    public static CardProperties None { get; } = new();
}

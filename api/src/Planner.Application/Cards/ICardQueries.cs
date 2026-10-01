namespace Planner.Application.Cards;

/// <summary>Leitura: cartões direto no formato da resposta.</summary>
public interface ICardQueries {
    /// <summary>Os cartões na raiz do quadro da categoria; nulo se a categoria não existir ou não for do usuário.</summary>
    Task<IReadOnlyList<CardResponse>?> ListRootAsync(Guid userId, Guid categoryId, CancellationToken ct);

    /// <summary>Os cartões no quadro de um cartão; nulo se o cartão não existir ou não for do usuário.</summary>
    Task<IReadOnlyList<CardResponse>?> ListChildrenAsync(Guid userId, Guid cardId, CancellationToken ct);

    /// <summary>O cartão completo, com os ancestrais (para a trilha de navegação).</summary>
    Task<CardResponse?> FindAsync(Guid userId, Guid id, CancellationToken ct);
}

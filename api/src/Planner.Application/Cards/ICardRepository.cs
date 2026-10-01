using Planner.Domain.Cards;

namespace Planner.Application.Cards;

/// <summary>Escrita: carrega e guarda cartões como entidades do domínio.</summary>
public interface ICardRepository {
    /// <summary>O cartão, se existir e for do usuário (pela categoria dele).</summary>
    Task<Card?> FindAsync(Guid userId, Guid id, CancellationToken ct);

    /// <summary>Se existe um cartão com esse id, de qualquer usuário.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    /// <summary>A camada para um cartão novo nesse quadro: na frente de todos.</summary>
    Task<int> NextLayerAsync(Guid categoryId, Guid? parentId, CancellationToken ct);

    void Add(Card card);

    void Remove(Card card);

    /// <summary>A gravação só vale se o cartão ainda estiver nessa versão (If-Match); senão, 412.</summary>
    void ExpectVersion(Card card, string version);
}

using Planner.Application.Categories;
using Planner.Application.Common;
using Planner.Domain.Cards;

namespace Planner.Application.Cards;

/// <summary>Os casos de uso dos cartões, sempre dentro das categorias do usuário atual.</summary>
public sealed class CardUseCases(
    ICurrentUser user,
    ICardRepository cards,
    ICategoryRepository categories,
    ICardQueries queries,
    IUnitOfWork unitOfWork) {
    public async Task<IReadOnlyList<CardResponse>> ListRootAsync(Guid categoryId, CancellationToken ct) =>
        await queries.ListRootAsync(user.Id, categoryId, ct) ?? throw UseCaseException.NotFound("Category");

    public async Task<IReadOnlyList<CardResponse>> ListChildrenAsync(Guid cardId, CancellationToken ct) =>
        await queries.ListChildrenAsync(user.Id, cardId, ct) ?? throw UseCaseException.NotFound("Card");

    public async Task<CardResponse> GetAsync(Guid id, CancellationToken ct) =>
        await queries.FindAsync(user.Id, id, ct) ?? throw UseCaseException.NotFound("Card");

    /// <summary>
    /// Cria o cartão com o id escolhido pelo cliente, na frente dos outros do quadro. Repetir o pedido com o mesmo id
    /// devolve o cartão já criado (<c>Created</c> falso), em vez de duplicá-lo.
    /// </summary>
    public async Task<(CardResponse Card, bool Created)> CreateAsync(
        CardLocation location, Guid id, string title, Position position, CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(location);
        if(await queries.FindAsync(user.Id, id, ct) is { } existing) {
            return (existing, false);
        }
        if(await cards.ExistsAsync(id, ct)) {
            throw new UseCaseException(UseCaseErrorKind.Conflict, "id-taken", "This id is already in use.");
        }

        var card = location.ParentId is { } parentId
            ? Card.CreateInside(id, await FindOwnAsync(parentId, ifMatch: null, ct), title)
            : Card.Create(id, await RequireOwnCategoryAsync(location.CategoryId!.Value, ct), title);
        card.MoveTo(position);
        card.ChangeLayer(await cards.NextLayerAsync(card.CategoryId, card.ParentId, ct));
        cards.Add(card);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetAsync(id, ct), true);
    }

    /// <summary>Aplica as alterações enviadas. Com <paramref name="ifMatch"/>, só se ninguém mudou o cartão antes.</summary>
    public async Task<CardResponse> UpdateAsync(Guid id, CardChanges changes, string? ifMatch, CancellationToken ct) {
        ArgumentNullException.ThrowIfNull(changes);
        var card = await FindOwnAsync(id, ifMatch, ct);
        if(changes.Title is { } title) {
            card.Rename(title);
        }
        if(changes.Color is { } color) {
            card.ChangeColor(color);
        }
        if(changes.Position is { } position) {
            card.MoveTo(position);
        }
        if(changes.Size is { } size) {
            card.Resize(size);
        }
        if(changes.Content is { } content) {
            card.ChangeContent(content.Json);
        }
        if(changes.Properties is { } properties) {
            card.ChangeProperties(properties);
        }
        if(changes.Location is { } location) {
            await MoveAsync(card, location, ct);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Apaga o cartão e, em cascata no banco, tudo o que está dentro dele.</summary>
    public async Task DeleteAsync(Guid id, string? ifMatch, CancellationToken ct) {
        cards.Remove(await FindOwnAsync(id, ifMatch, ct));
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Move para dentro de outro cartão ou para a raiz de uma categoria, sempre do próprio usuário. O domínio barra o
    /// ciclo direto; ciclos mais profundos são barrados pelo trigger do banco (viram 409 card.cycle).
    /// </summary>
    private async Task MoveAsync(Card card, CardLocation location, CancellationToken ct) {
        if(location.ParentId is { } parentId) {
            card.MoveInto(await FindOwnAsync(parentId, ifMatch: null, ct));
        }
        else {
            card.MoveToRoot(await RequireOwnCategoryAsync(location.CategoryId!.Value, ct));
        }
    }

    private async Task<Card> FindOwnAsync(Guid id, string? ifMatch, CancellationToken ct) {
        var card = await cards.FindAsync(user.Id, id, ct) ?? throw UseCaseException.NotFound("Card");
        if(ifMatch is not null) {
            cards.ExpectVersion(card, ifMatch);
        }
        return card;
    }

    private async Task<Guid> RequireOwnCategoryAsync(Guid categoryId, CancellationToken ct) =>
        (await categories.FindAsync(user.Id, categoryId, ct) ?? throw UseCaseException.NotFound("Category")).Id;
}

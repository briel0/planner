using Planner.Application.Common;
using Planner.Domain.Categories;

namespace Planner.Application.Categories;

/// <summary>Os casos de uso das categorias (as abas do rodapé), sempre do usuário atual.</summary>
public sealed class CategoryUseCases(
    ICurrentUser user,
    ICategoryRepository categories,
    ICategoryQueries queries,
    IUnitOfWork unitOfWork) {
    public Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct) => queries.ListAsync(user.Id, ct);

    public async Task<CategoryResponse> GetAsync(Guid id, CancellationToken ct) =>
        await queries.FindAsync(user.Id, id, ct) ?? throw UseCaseException.NotFound("Category");

    /// <summary>
    /// Cria a categoria com o id escolhido pelo cliente. Repetir o pedido com o mesmo id devolve a categoria já
    /// criada (<c>Created</c> falso), em vez de duplicá-la.
    /// </summary>
    public async Task<(CategoryResponse Category, bool Created)> CreateAsync(Guid id, string name, CancellationToken ct) {
        if(await queries.FindAsync(user.Id, id, ct) is { } existing) {
            return (existing, false);
        }
        if(await categories.ExistsAsync(id, ct)) {
            throw new UseCaseException(UseCaseErrorKind.Conflict, "id-taken", "This id is already in use.");
        }

        var category = Category.Create(id, user.Id, name, await categories.NextSortOrderAsync(user.Id, ct));
        await EnsureNameIsFreeAsync(category, ct);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(ct);
        return (await GetAsync(id, ct), true);
    }

    /// <summary>Renomeia e/ou reposiciona a aba. Com <paramref name="ifMatch"/>, só se ninguém a mudou antes.</summary>
    public async Task<CategoryResponse> UpdateAsync(
        Guid id, string? name, int? sortOrder, string? ifMatch, CancellationToken ct) {
        var category = await FindOwnAsync(id, ifMatch, ct);
        if(name is not null) {
            category.Rename(name);
            await EnsureNameIsFreeAsync(category, ct);
        }
        if(sortOrder is { } order) {
            category.ChangeSortOrder(order);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Apaga a categoria e, em cascata no banco, todos os cartões dela.</summary>
    public async Task DeleteAsync(Guid id, string? ifMatch, CancellationToken ct) {
        categories.Remove(await FindOwnAsync(id, ifMatch, ct));
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Category> FindOwnAsync(Guid id, string? ifMatch, CancellationToken ct) {
        var category = await categories.FindAsync(user.Id, id, ct) ?? throw UseCaseException.NotFound("Category");
        if(ifMatch is not null) {
            categories.ExpectVersion(category, ifMatch);
        }
        return category;
    }

    private async Task EnsureNameIsFreeAsync(Category category, CancellationToken ct) {
        if(await categories.NameTakenAsync(user.Id, category.Name, category.Id, ct)) {
            throw new UseCaseException(
                UseCaseErrorKind.Conflict, "category.name-taken", $"A category named '{category.Name}' already exists.");
        }
    }
}

using Planner.Domain.Categories;

namespace Planner.Application.Categories;

/// <summary>Escrita: carrega e guarda categorias como entidades do domínio.</summary>
public interface ICategoryRepository {
    /// <summary>A categoria, se existir e for do usuário.</summary>
    Task<Category?> FindAsync(Guid userId, Guid id, CancellationToken ct);

    /// <summary>Se existe uma categoria com esse id, de qualquer usuário.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    /// <summary>Se o usuário já tem outra categoria com esse nome (ignorando maiúsculas/minúsculas).</summary>
    Task<bool> NameTakenAsync(Guid userId, string name, Guid ignoreId, CancellationToken ct);

    /// <summary>A posição para uma aba nova: depois da última.</summary>
    Task<int> NextSortOrderAsync(Guid userId, CancellationToken ct);

    void Add(Category category);

    void Remove(Category category);

    /// <summary>A gravação só vale se a categoria ainda estiver nessa versão (If-Match); senão, 412.</summary>
    void ExpectVersion(Category category, string version);
}

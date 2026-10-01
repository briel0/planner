namespace Planner.Application.Categories;

/// <summary>Leitura: categorias direto no formato da resposta (com datas e versão, que só o banco tem).</summary>
public interface ICategoryQueries {
    /// <summary>As categorias do usuário, na ordem das abas.</summary>
    Task<IReadOnlyList<CategoryResponse>> ListAsync(Guid userId, CancellationToken ct);

    Task<CategoryResponse?> FindAsync(Guid userId, Guid id, CancellationToken ct);
}

namespace Planner.Api.Categories;

/// <param name="Id">Gerado pelo cliente (UUID v7): repetir o pedido com o mesmo id não duplica a categoria.</param>
/// <param name="Name">Nome da aba: 1 a 100 caracteres, único entre as categorias do usuário.</param>
public sealed record CreateCategoryRequest(Guid Id, string Name);

/// <summary>Só os campos enviados são alterados (todos opcionais).</summary>
public sealed record UpdateCategoryRequest {
    public string? Name { get; init; }

    public int? SortOrder { get; init; }
}

namespace Planner.Application.Categories;

/// <summary>Uma categoria como a API a devolve (docs/api-design.md).</summary>
/// <param name="Version">Versão da categoria, para o If-Match (concorrência otimista).</param>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Version);

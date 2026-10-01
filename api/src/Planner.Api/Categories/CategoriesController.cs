using Microsoft.AspNetCore.Mvc;
using Planner.Application.Categories;

namespace Planner.Api.Categories;

/// <summary>As categorias (abas do rodapé) do usuário atual.</summary>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public sealed class CategoriesController(CategoryUseCases categories) : ControllerBase {
    /// <summary>As categorias, na ordem das abas.</summary>
    [HttpGet]
    public Task<IReadOnlyList<CategoryResponse>> List(CancellationToken ct) => categories.ListAsync(ct);

    [HttpGet("{id:guid}", Name = nameof(GetCategory))]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategory(Guid id, CancellationToken ct) =>
        WithETag(await categories.GetAsync(id, ct));

    /// <summary>Cria uma categoria. Idempotente: repetir com o mesmo id devolve a categoria existente (200).</summary>
    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken ct) {
        var (category, created) = await categories.CreateAsync(request.Id, request.Name, ct);
        WithETag(category);
        return created ? CreatedAtRoute(nameof(GetCategory), new { id = category.Id }, category) : Ok(category);
    }

    /// <summary>Renomeia e/ou reposiciona. Com If-Match, só se a categoria ainda estiver naquela versão (senão 412).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid id, UpdateCategoryRequest request, CancellationToken ct) =>
        WithETag(await categories.UpdateAsync(id, request.Name, request.SortOrder, IfMatch(), ct));

    /// <summary>Apaga a categoria e todos os cartões dela.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) {
        await categories.DeleteAsync(id, IfMatch(), ct);
        return NoContent();
    }

    /// <summary>A versão no cabeçalho ETag (entre aspas, como manda o HTTP), além do campo <c>version</c>.</summary>
    private CategoryResponse WithETag(CategoryResponse category) {
        Response.Headers.ETag = $"\"{category.Version}\"";
        return category;
    }

    /// <summary>O If-Match sem as aspas; ausente ou "*" significa "sem condição".</summary>
    private string? IfMatch() {
        var value = Request.Headers.IfMatch.ToString();
        return value is "" or "*" ? null : value.Trim('"');
    }
}

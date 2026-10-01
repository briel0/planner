using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Planner.Application.Cards;
using Planner.Domain.Cards;

namespace Planner.Api.Cards;

/// <summary>Os cartões e os quadros (o da raiz de cada categoria e o de dentro de cada cartão).</summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public sealed class CardsController(CardUseCases cards) : ControllerBase {
    /// <summary>Os cartões na raiz do quadro de uma categoria.</summary>
    [HttpGet("api/categories/{categoryId:guid}/cards", Name = "ListRootCards")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<CardResponse>> ListRoot(Guid categoryId, CancellationToken ct) =>
        cards.ListRootAsync(categoryId, ct);

    /// <summary>Cria um cartão na raiz do quadro de uma categoria. Idempotente (repetir com o mesmo id devolve 200).</summary>
    [HttpPost("api/categories/{categoryId:guid}/cards", Name = "CreateRootCard")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CardResponse>> CreateRoot(
        Guid categoryId, CreateCardRequest request, CancellationToken ct) =>
        Create(new CardLocation(categoryId, null), request, ct);

    /// <summary>Os cartões no quadro de um cartão.</summary>
    [HttpGet("api/cards/{id:guid}/children", Name = "ListChildCards")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<CardResponse>> ListChildren(Guid id, CancellationToken ct) =>
        cards.ListChildrenAsync(id, ct);

    /// <summary>Cria um cartão dentro de outro (na mesma categoria). Idempotente (repetir com o mesmo id devolve 200).</summary>
    [HttpPost("api/cards/{id:guid}/children", Name = "CreateChildCard")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<CardResponse>> CreateChild(Guid id, CreateCardRequest request, CancellationToken ct) =>
        Create(new CardLocation(null, id), request, ct);

    /// <summary>O cartão completo, com os ancestrais (para a trilha de navegação).</summary>
    [HttpGet("api/cards/{id:guid}", Name = "GetCard")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CardResponse>> Get(Guid id, CancellationToken ct) =>
        WithETag(await cards.GetAsync(id, ct));

    /// <summary>
    /// Altera os campos enviados, ou move o cartão (<c>location</c>). Com If-Match, só se o cartão ainda estiver
    /// naquela versão (senão 412). Mover para dentro de um descendente é recusado (409 card.cycle).
    /// </summary>
    [HttpPatch("api/cards/{id:guid}", Name = "UpdateCard")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<CardResponse>> Update(
        Guid id,
        UpdateCardRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct) =>
        WithETag(await cards.UpdateAsync(id, ToChanges(request), Version(ifMatch), ct));

    /// <summary>Apaga o cartão e tudo o que está dentro dele.</summary>
    [HttpDelete("api/cards/{id:guid}", Name = "DeleteCard")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> Delete(
        Guid id, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) {
        await cards.DeleteAsync(id, Version(ifMatch), ct);
        return NoContent();
    }

    private async Task<ActionResult<CardResponse>> Create(
        CardLocation location, CreateCardRequest request, CancellationToken ct) {
        var (card, created) = await cards.CreateAsync(
            location, request.Id, request.Title, new Position(request.Position.X, request.Position.Y), ct);
        WithETag(card);
        return created ? CreatedAtRoute("GetCard", new { id = card.Id }, card) : Ok(card);
    }

    /// <summary>Do formato HTTP para os objetos do domínio (que validam a si mesmos: posição, tamanho...).</summary>
    private static CardChanges ToChanges(UpdateCardRequest request) => new(
        request.Title,
        request.Color,
        request.Position is { } position ? new Position(position.X, position.Y) : null,
        request.Size is { } size ? new Size(size.Width, size.Height) : null,
        request.Content.ValueKind switch {
            JsonValueKind.Undefined => null, // ausente: não muda
            JsonValueKind.Null => new ContentChange(null), // null: apaga a descrição
            _ => new ContentChange(request.Content.GetRawText()),
        },
        request.Properties is { } properties ? new CardProperties(properties.DueOn, properties.Done) : null,
        request.Location is { } location ? CardLocation.Validate(location.CategoryId, location.ParentId) : null);

    private CardResponse WithETag(CardResponse card) {
        Response.Headers.ETag = $"\"{card.Version}\"";
        return card;
    }

    private static string? Version(string? ifMatch) =>
        string.IsNullOrWhiteSpace(ifMatch) || ifMatch == "*" ? null : ifMatch.Trim().Trim('"');
}

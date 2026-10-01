using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Planner.Application.Cards;
using Planner.Application.Categories;
using Planner.Domain.Cards;
using Planner.Domain.Categories;
using Planner.Domain.Users;

namespace Planner.IntegrationTests.Api;

/// <summary>Os endpoints de cartões de ponta a ponta (HTTP → casos de uso → Postgres).</summary>
[Collection(nameof(PlannerTestGroup))]
public class CardsApiTests(PlannerFixture planner) {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = planner.Api.CreateClient();

    [Fact]
    public async Task Boards_list_summaries_and_a_card_comes_with_its_ancestors() {
        var category = await CreateCategoryAsync();
        var root = await CreateCardAsync($"/api/categories/{category.Id}/cards", "Física Quântica");
        var child = await CreateCardAsync($"/api/cards/{root.Id}/children", "Lista 3");
        var grandchild = await CreateCardAsync($"/api/cards/{child.Id}/children", "Exercício 5");

        var board = await GetJsonAsync($"/api/categories/{category.Id}/cards");
        var details = await GetCardAsync(grandchild.Id);

        var summary = Assert.Single(board.EnumerateArray());
        Assert.Equal(1, summary.GetProperty("childCount").GetInt32());
        Assert.False(summary.TryGetProperty("ancestors", out _)); // listas trazem só o resumo
        Assert.Equal([root.Title, child.Title], details.Ancestors?.Select(a => a.Title));
        Assert.Equal(category.Id, details.CategoryId);
    }

    [Fact]
    public async Task Create_is_idempotent() {
        var category = await CreateCategoryAsync();
        var request = new { id = Guid.CreateVersion7(), title = "Lista 3", position = new { x = 10, y = 20 } };

        var first = await _client.PostAsJsonAsync($"/api/categories/{category.Id}/cards", request, Ct);
        var repeated = await _client.PostAsJsonAsync($"/api/categories/{category.Id}/cards", request, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Single((await GetJsonAsync($"/api/categories/{category.Id}/cards")).EnumerateArray());
    }

    [Fact]
    public async Task Update_changes_only_the_sent_fields_and_null_content_clears_it() {
        var card = await CreateCardAsync($"/api/categories/{(await CreateCategoryAsync()).Id}/cards", "Lista 3");

        var updated = await ReadCardAsync(await PatchAsync(card.Id, new {
            color = "#3B82F6",
            size = new { width = 300, height = 150 },
            content = new { type = "doc", content = Array.Empty<object>() },
            properties = new { dueOn = "2026-09-30" },
        }));
        var renamed = await ReadCardAsync(await PatchAsync(card.Id, new { title = "Lista 4" }));
        var cleared = await ReadCardAsync(await PatchAsync(card.Id, new { content = (object?)null }));

        Assert.Equal("#3b82f6", updated.Color);
        Assert.Equal(new SizeDto(300, 150), updated.Size);
        Assert.Equal(new DateOnly(2026, 9, 30), updated.Properties.DueOn);
        Assert.Null(updated.Properties.Done);
        Assert.NotNull(renamed.Content); // ausente no PATCH: não mudou
        Assert.Equal("#3b82f6", renamed.Color);
        Assert.Null(cleared.Content); // null no PATCH: apagou
    }

    [Fact]
    public async Task Invalid_due_dates_are_rejected() {
        var card = await CreateCardAsync($"/api/categories/{(await CreateCategoryAsync()).Id}/cards", "Lista 3");

        var response = await PatchAsync(card.Id, new { properties = new { dueOn = "amanhã" } });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation");
    }

    [Fact]
    public async Task Moving_a_card_inside_one_of_its_descendants_is_refused() {
        var category = await CreateCategoryAsync();
        var a = await CreateCardAsync($"/api/categories/{category.Id}/cards", "A");
        var b = await CreateCardAsync($"/api/cards/{a.Id}/children", "B");
        var c = await CreateCardAsync($"/api/cards/{b.Id}/children", "C");

        var response = await PatchAsync(a.Id, new { location = new { parentId = c.Id } });

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "card.cycle");
    }

    [Fact]
    public async Task Moving_a_card_to_another_category_takes_its_whole_subtree() {
        var from = await CreateCategoryAsync();
        var to = await CreateCategoryAsync();
        var root = await CreateCardAsync($"/api/categories/{from.Id}/cards", "Física Quântica");
        var child = await CreateCardAsync($"/api/cards/{root.Id}/children", "Lista 3");

        var moved = await ReadCardAsync(await PatchAsync(root.Id, new { location = new { categoryId = to.Id } }));

        Assert.Equal(to.Id, moved.CategoryId);
        Assert.Equal(to.Id, (await GetCardAsync(child.Id)).CategoryId);
        Assert.Empty((await GetJsonAsync($"/api/categories/{from.Id}/cards")).EnumerateArray());
    }

    [Fact]
    public async Task A_stale_version_is_refused() {
        var card = await CreateCardAsync($"/api/categories/{(await CreateCategoryAsync()).Id}/cards", "Lista 3");
        await PatchAsync(card.Id, new { title = "Lista 4" }, ifMatch: card.Version);

        var stale = await PatchAsync(card.Id, new { title = "Lista 5" }, ifMatch: card.Version);

        await AssertProblemAsync(stale, HttpStatusCode.PreconditionFailed, "concurrency.stale");
    }

    [Fact]
    public async Task Another_users_cards_are_not_found() {
        var otherUser = User.Create("Outra pessoa", new Email($"{Guid.NewGuid():N}@example.com"));
        var theirCategory = Category.Create(Guid.CreateVersion7(), otherUser.Id, "Particular", sortOrder: 0);
        var theirCard = Card.Create(Guid.CreateVersion7(), theirCategory.Id, "Segredo");
        await using(var db = planner.NewContext()) {
            db.AddRange(otherUser, theirCategory, theirCard);
            await db.SaveChangesAsync(Ct);
        }

        await AssertProblemAsync(await _client.GetAsync($"/api/cards/{theirCard.Id}", Ct), HttpStatusCode.NotFound, "not-found");
        await AssertProblemAsync(
            await _client.GetAsync($"/api/categories/{theirCategory.Id}/cards", Ct), HttpStatusCode.NotFound, "not-found");
        await AssertProblemAsync(
            await _client.DeleteAsync($"/api/cards/{theirCard.Id}", Ct), HttpStatusCode.NotFound, "not-found");
    }

    [Fact]
    public async Task Deleting_a_card_deletes_its_whole_subtree() {
        var category = await CreateCategoryAsync();
        var root = await CreateCardAsync($"/api/categories/{category.Id}/cards", "Física Quântica");
        var child = await CreateCardAsync($"/api/cards/{root.Id}/children", "Lista 3");

        var delete = await _client.DeleteAsync($"/api/cards/{root.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/cards/{child.Id}", Ct)).StatusCode);
    }

    private async Task<CategoryResponse> CreateCategoryAsync() {
        var response = await _client.PostAsJsonAsync(
            "/api/categories", new { id = Guid.CreateVersion7(), name = $"Categoria {Guid.NewGuid():N}"[..30] }, Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryResponse>(Ct) ?? throw new InvalidOperationException();
    }

    private async Task<CardResponse> CreateCardAsync(string path, string title) {
        var response = await _client.PostAsJsonAsync(
            path, new { id = Guid.CreateVersion7(), title, position = new { x = 0, y = 0 } }, Ct);
        response.EnsureSuccessStatusCode();
        return await ReadCardAsync(response);
    }

    private async Task<HttpResponseMessage> PatchAsync(Guid id, object body, string? ifMatch = null) {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/cards/{id}") {
            Content = JsonContent.Create(body),
        };
        if(ifMatch is not null) {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{ifMatch}\""));
        }
        return await _client.SendAsync(request, Ct);
    }

    private async Task<CardResponse> GetCardAsync(Guid id) =>
        await ReadCardAsync(await _client.GetAsync($"/api/cards/{id}", Ct));

    private async Task<JsonElement> GetJsonAsync(string path) =>
        await _client.GetFromJsonAsync<JsonElement>(path, Ct);

    private static async Task<CardResponse> ReadCardAsync(HttpResponseMessage response) {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CardResponse>(Ct) ?? throw new InvalidOperationException();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code) {
        Assert.Equal(status, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
    }
}

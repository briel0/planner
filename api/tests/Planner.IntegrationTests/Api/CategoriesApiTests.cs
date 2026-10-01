using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Planner.Application.Categories;
using Planner.Domain.Categories;
using Planner.Domain.Users;

namespace Planner.IntegrationTests.Api;

/// <summary>
/// Os endpoints de categorias de ponta a ponta (HTTP → casos de uso → Postgres), com as convenções de
/// docs/api-design.md: criação idempotente, Problem Details com <c>code</c>, If-Match e 404 para dados alheios.
/// </summary>
[Collection(nameof(PlannerTestGroup))]
public class CategoriesApiTests(PlannerFixture planner) {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HttpClient _client = planner.Api.CreateClient();

    [Fact]
    public async Task Create_is_idempotent() {
        var request = new { id = Guid.CreateVersion7(), name = UniqueName() };

        var first = await _client.PostAsJsonAsync("/api/categories", request, Ct);
        var repeated = await _client.PostAsJsonAsync("/api/categories", request, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal($"/api/categories/{request.id}", first.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        var created = await ReadAsync(first);
        Assert.Equal(created, await ReadAsync(repeated));
        Assert.Single((await ListAsync()), c => c.Id == request.id);
    }

    [Fact]
    public async Task Create_rejects_a_name_already_used_ignoring_letter_case() {
        var name = UniqueName();
        await CreateAsync(name);

        var response = await _client.PostAsJsonAsync(
            "/api/categories", new { id = Guid.CreateVersion7(), name = name.ToUpperInvariant() }, Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "category.name-taken");
    }

    [Fact]
    public async Task Create_rejects_ids_that_are_not_uuid_version_7() {
        var response = await _client.PostAsJsonAsync(
            "/api/categories", new { id = Guid.NewGuid(), name = UniqueName() }, Ct);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "invalid-id");
    }

    [Fact]
    public async Task Update_with_a_stale_version_is_refused_and_with_the_current_one_succeeds() {
        var category = await CreateAsync(UniqueName());
        var renamed = await PatchAsync(category.Id, new { name = UniqueName() }, ifMatch: category.Version);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var current = await ReadAsync(renamed);
        Assert.NotEqual(category.Version, current.Version);
        Assert.Equal($"\"{current.Version}\"", renamed.Headers.ETag?.Tag);

        var stale = await PatchAsync(category.Id, new { name = UniqueName() }, ifMatch: category.Version);

        await AssertProblemAsync(stale, HttpStatusCode.PreconditionFailed, "concurrency.stale");
    }

    [Fact]
    public async Task Update_without_if_match_applies_only_the_sent_fields() {
        var category = await CreateAsync(UniqueName());

        var response = await PatchAsync(category.Id, new { sortOrder = 42 }, ifMatch: null);

        var updated = await ReadAsync(response);
        Assert.Equal(42, updated.SortOrder);
        Assert.Equal(category.Name, updated.Name);
    }

    [Fact]
    public async Task Another_users_category_is_not_found() {
        var otherUser = User.Create("Outra pessoa", new Email($"{Guid.NewGuid():N}@example.com"));
        var theirs = Category.Create(Guid.CreateVersion7(), otherUser.Id, "Particular", sortOrder: 0);
        await using(var db = planner.NewContext()) {
            db.AddRange(otherUser, theirs);
            await db.SaveChangesAsync(Ct);
        }

        var get = await _client.GetAsync($"/api/categories/{theirs.Id}", Ct);
        var delete = await _client.DeleteAsync($"/api/categories/{theirs.Id}", Ct);

        await AssertProblemAsync(get, HttpStatusCode.NotFound, "not-found");
        await AssertProblemAsync(delete, HttpStatusCode.NotFound, "not-found");
        Assert.DoesNotContain(await ListAsync(), c => c.Id == theirs.Id);
    }

    [Fact]
    public async Task Delete_removes_the_category() {
        var category = await CreateAsync(UniqueName());

        var delete = await _client.DeleteAsync($"/api/categories/{category.Id}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var get = await _client.GetAsync($"/api/categories/{category.Id}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    private static string UniqueName() => $"Categoria {Guid.NewGuid():N}"[..30];

    private async Task<CategoryResponse> CreateAsync(string name) {
        var response = await _client.PostAsJsonAsync("/api/categories", new { id = Guid.CreateVersion7(), name }, Ct);
        response.EnsureSuccessStatusCode();
        return await ReadAsync(response);
    }

    private async Task<HttpResponseMessage> PatchAsync(Guid id, object body, string? ifMatch) {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/categories/{id}") {
            Content = JsonContent.Create(body),
        };
        if(ifMatch is not null) {
            request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{ifMatch}\""));
        }
        return await _client.SendAsync(request, Ct);
    }

    private async Task<List<CategoryResponse>> ListAsync() =>
        await _client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories", Ct) ?? [];

    private static async Task<CategoryResponse> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<CategoryResponse>(Ct)
            ?? throw new InvalidOperationException("Empty response.");

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code) {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
    }
}

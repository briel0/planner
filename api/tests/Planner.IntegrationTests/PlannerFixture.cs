using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Planner.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Planner.IntegrationTests;

/// <summary>
/// Um PostgreSQL 18 descartável (num container) com as migrations aplicadas, e a API de verdade apontando para
/// ele. Compartilhado por todos os testes de integração (um container só, para poupar memória); por isso cada
/// teste cria os próprios dados. Precisa de Docker.
/// </summary>
public sealed class PlannerFixture : IAsyncLifetime {
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();
    private WebApplicationFactory<Program>? _api;

    /// <summary>A API em memória, no ambiente Development (autenticada como o usuário dev).</summary>
    public WebApplicationFactory<Program> Api => _api ?? throw new InvalidOperationException("Not initialized.");

    public async ValueTask InitializeAsync() {
        await _container.StartAsync();
        await using(var db = NewContext()) {
            await db.Database.MigrateAsync();
        }
        _api = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host
            .UseEnvironment("Development")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Planner"] = _container.GetConnectionString() })));
    }

    public async ValueTask DisposeAsync() {
        if(_api is not null) {
            await _api.DisposeAsync();
        }
        await _container.DisposeAsync();
    }

    /// <summary>Um contexto novo, sem nada em memória: o que ele lê veio de fato do banco.</summary>
    public PlannerDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PlannerDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options);
}

[CollectionDefinition(nameof(PlannerTestGroup))]
public sealed class PlannerTestGroup : ICollectionFixture<PlannerFixture>;

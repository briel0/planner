using Microsoft.EntityFrameworkCore;
using Planner.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Planner.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Sobe um PostgreSQL 18 descartável num container, aplica as migrations e o derruba no fim dos testes.
/// Precisa de Docker. Os testes compartilham o mesmo banco, então cada um cria os próprios dados.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime {
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    public async ValueTask InitializeAsync() {
        await _container.StartAsync();
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Um contexto novo, sem nada em memória: o que ele lê veio de fato do banco.</summary>
    public PlannerDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PlannerDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planner.Infrastructure.Persistence;

namespace Planner.Infrastructure;

public static class DependencyInjection {
    /// <summary>Registra os serviços da camada de infraestrutura (banco de dados).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        var connectionString = configuration.GetConnectionString("Planner")
            ?? throw new InvalidOperationException(
                "Connection string 'Planner' is not configured. See 'Primeira configuração' in CLAUDE.md.");

        services.AddDbContext<PlannerDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        return services;
    }
}

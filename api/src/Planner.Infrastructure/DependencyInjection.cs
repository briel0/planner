using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Planner.Application.Categories;
using Planner.Application.Common;
using Planner.Infrastructure.Persistence;
using Planner.Infrastructure.Persistence.Categories;

namespace Planner.Infrastructure;

public static class DependencyInjection {
    /// <summary>Registra os serviços da camada de infraestrutura (banco de dados).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        // A string de conexão só é exigida quando o banco é usado de fato (não na geração do OpenAPI no build).
        services.AddDbContext<PlannerDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Planner")
                ?? throw new InvalidOperationException(
                    "Connection string 'Planner' is not configured. See 'Primeira configuração' in CLAUDE.md."))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryQueries, CategoryQueries>();

        return services;
    }
}

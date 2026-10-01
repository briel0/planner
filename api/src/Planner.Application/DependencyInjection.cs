using Microsoft.Extensions.DependencyInjection;
using Planner.Application.Categories;

namespace Planner.Application;

public static class DependencyInjection {
    /// <summary>Registra os casos de uso. As interfaces que eles usam são implementadas pela Infrastructure.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        services.AddScoped<CategoryUseCases>();
        return services;
    }
}

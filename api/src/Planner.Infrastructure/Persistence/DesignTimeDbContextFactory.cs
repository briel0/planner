using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planner.Infrastructure.Persistence;

/// <summary>
/// Usado só pelas ferramentas do EF Core (<c>dotnet ef migrations add</c>), que não se conectam ao banco.
/// Para aplicar migrations, passe a string de conexão com <c>--connection</c>.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlannerDbContext> {
    public PlannerDbContext CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<PlannerDbContext>()
            .UseNpgsql("Host=localhost;Database=planner")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PlannerDbContext(options);
    }
}

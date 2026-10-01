using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Planner.Domain.Cards;
using Planner.Domain.Categories;
using Planner.Domain.Users;

namespace Planner.Infrastructure.Persistence;

/// <summary>
/// O banco visto pelo C#: cada <see cref="DbSet{TEntity}"/> é uma tabela. O mapeamento de cada entidade fica
/// em <c>Configurations/</c>; o modelo físico de referência é docs/data-modeling/04-physical-model.md.
/// </summary>
public sealed class PlannerDbContext(DbContextOptions<PlannerDbContext> options) : DbContext(options) {
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Card> Cards => Set<Card>();

    /// <summary>
    /// Sem índices automáticos em chaves estrangeiras: cada índice é declarado de propósito, a partir das
    /// consultas reais (ver a seção Indexes do modelo físico).
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlannerDbContext).Assembly);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Planner.Infrastructure.Persistence.Configurations;

internal static class Timestamps {
    /// <summary>
    /// Colunas <c>created_at</c> e <c>updated_at</c>: existem só no banco (o domínio não as conhece). O banco
    /// preenche as duas (valor padrão e trigger), e o EF Core apenas as lê.
    /// </summary>
    public static void HasTimestamps<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : class {
        builder.Property<DateTimeOffset>("CreatedAt").HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property<DateTimeOffset>("UpdatedAt").HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
    }
}

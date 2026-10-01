using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planner.Domain.Categories;
using Planner.Domain.Users;

namespace Planner.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category> {
    public void Configure(EntityTypeBuilder<Category> builder) {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("uuidv7()");

        builder.Property(c => c.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        // O índice único (user_id, lower(name)) é criado na migration: o EF Core não gera índices de expressão.
        builder.ToTable(table =>
            table.HasCheckConstraint("ck_categories_name", Checks.RequiredText("name", Category.NameMaxLength)));

        builder.HasVersion();
        builder.HasTimestamps();
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Planner.Domain.Cards;
using Planner.Domain.Categories;

namespace Planner.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card> {
    /// <summary>Chaves do catálogo em snake_case; propriedades ausentes (nulas) não são gravadas.</summary>
    private static readonly JsonSerializerOptions PropertiesJson = new() {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Configure(EntityTypeBuilder<Card> builder) {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("uuidv7()");

        builder.HasOne<Category>().WithMany().HasForeignKey(c => c.CategoryId).OnDelete(DeleteBehavior.Cascade);

        // Pai simples, para o EF Core ordenar as inserções (pai antes do filho). A FK composta
        // (parent_id, category_id) → (id, category_id), que garante "mesma categoria do pai", é criada na
        // migration: no EF Core, chaves alternativas não podem mudar, e a categoria de um cartão muda.
        builder.HasOne<Card>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Color).HasDefaultValue(Card.DefaultColor);

        // Position (objeto de valor) ↔ duas colunas.
        builder.ComplexProperty(c => c.Position, position => {
            position.Property(p => p.X).HasColumnName("position_x");
            position.Property(p => p.Y).HasColumnName("position_y");
        });

        // Size (objeto de valor) ↔ duas colunas. O valor padrão vale para os cartões que já existiam.
        builder.ComplexProperty(c => c.Size, size => {
            size.Property(s => s.Width).HasColumnName("width").HasDefaultValue(Size.Default.Width);
            size.Property(s => s.Height).HasColumnName("height").HasDefaultValue(Size.Default.Height);
        });

        builder.Property(c => c.Content).HasColumnType("jsonb");

        // CardProperties (objeto de valor) ↔ uma coluna JSONB, ex.: {"due_on": "2026-09-30", "done": false}.
        builder.Property(c => c.Properties)
            .HasColumnType("jsonb")
            .HasConversion(
                properties => JsonSerializer.Serialize(properties, PropertiesJson),
                json => JsonSerializer.Deserialize<CardProperties>(json, PropertiesJson) ?? CardProperties.None)
            .HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(c => new { c.CategoryId, c.ParentId });
        builder.HasIndex(c => new { c.ParentId, c.CategoryId });

        builder.ToTable(table => {
            table.HasCheckConstraint("ck_cards_title", Checks.RequiredText("title", Card.TitleMaxLength));
            table.HasCheckConstraint("ck_cards_color", "color ~ '^#[0-9a-f]{6}$'");
            table.HasCheckConstraint(
                "ck_cards_size",
                $"width BETWEEN {Size.MinWidth} AND {Size.MaxWidth} AND height BETWEEN {Size.MinHeight} AND {Size.MaxHeight}");
            table.HasCheckConstraint("ck_cards_content", "content IS NULL OR jsonb_typeof(content) = 'object'");
            table.HasCheckConstraint("ck_cards_properties", "jsonb_typeof(properties) = 'object'");
        });

        builder.HasVersion();
        builder.HasTimestamps();
    }
}

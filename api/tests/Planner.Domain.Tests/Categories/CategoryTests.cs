using Planner.Domain.Categories;

namespace Planner.Domain.Tests.Categories;

public class CategoryTests {
    private static Category Create(string name) =>
        Category.Create(Guid.CreateVersion7(), userId: Guid.CreateVersion7(), name, sortOrder: 0);

    [Fact]
    public void Create_trims_name() {
        var category = Create("  Faculdade  ");

        Assert.Equal("Faculdade", category.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string name) {
        var error = Assert.Throws<DomainException>(() => Create(name));

        Assert.Equal("category.invalid-name", error.Code);
    }

    [Fact]
    public void Create_accepts_name_with_100_characters() {
        var name = new string('a', 100);

        Assert.Equal(name, Create(name).Name);
    }

    [Fact]
    public void Create_rejects_name_longer_than_100_characters() {
        var error = Assert.Throws<DomainException>(() => Create(new string('a', 101)));

        Assert.Equal("category.invalid-name", error.Code);
    }

    [Fact]
    public void Create_keeps_the_id_chosen_by_the_client() {
        var id = Guid.CreateVersion7();

        var category = Category.Create(id, Guid.CreateVersion7(), "Faculdade", sortOrder: 0);

        Assert.Equal(id, category.Id);
    }

    [Fact]
    public void Create_rejects_ids_that_are_not_uuid_version_7() {
        var error = Assert.Throws<DomainException>(
            () => Category.Create(Guid.NewGuid(), Guid.CreateVersion7(), "Faculdade", sortOrder: 0)); // versão 4

        Assert.Equal("invalid-id", error.Code);
    }

    [Fact]
    public void Rename_applies_the_same_name_rules() {
        var category = Create("Faculdade");

        category.Rename("  Trabalho  ");

        Assert.Equal("Trabalho", category.Name);
        var error = Assert.Throws<DomainException>(() => category.Rename(""));
        Assert.Equal("category.invalid-name", error.Code);
    }
}

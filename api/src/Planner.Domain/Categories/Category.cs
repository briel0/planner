namespace Planner.Domain.Categories;

/// <summary>
/// Uma aba do rodapé, com o seu quadro. O nome é único entre as categorias do mesmo usuário, mas essa regra
/// depende das outras categorias e por isso é verificada fora do domínio (Application e índice único do banco).
/// </summary>
public sealed class Category {
    public const int NameMaxLength = 100;

    private Category(Guid id, Guid userId, string name, int sortOrder) {
        Id = id;
        UserId = userId;
        Name = name;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public string Name { get; private set; }

    /// <summary>Posição da aba no rodapé (menor vem antes).</summary>
    public int SortOrder { get; private set; }

    public static Category Create(Guid userId, string name, int sortOrder) =>
        new(Guid.CreateVersion7(), userId, NormalizeName(name), sortOrder);

    public void Rename(string name) => Name = NormalizeName(name);

    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;

    private static string NormalizeName(string name) =>
        RequiredText.Normalize(name, NameMaxLength, "category.invalid-name", "A category name");
}

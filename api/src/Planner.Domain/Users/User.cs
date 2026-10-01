namespace Planner.Domain.Users;

/// <summary>
/// Um usuário do planner. O e-mail é único entre os usuários, mas essa regra depende dos outros usuários e por
/// isso é verificada fora do domínio (Application e índice único do banco).
/// </summary>
public sealed class User {
    public const int NameMaxLength = 100;

    private User(Guid id, string name, Email email) {
        Id = id;
        Name = name;
        Email = email;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public Email Email { get; private set; }

    public static User Create(string name, Email email) => Create(Guid.CreateVersion7(), name, email);

    /// <param name="id">Um id conhecido de antemão (UUID v7), como o do usuário de desenvolvimento.</param>
    public static User Create(Guid id, string name, Email email) {
        ArgumentNullException.ThrowIfNull(email);
        return new(EntityId.Require(id), NormalizeName(name), email);
    }

    public void Rename(string name) => Name = NormalizeName(name);

    public void ChangeEmail(Email email) {
        ArgumentNullException.ThrowIfNull(email);
        Email = email;
    }

    private static string NormalizeName(string name) =>
        RequiredText.Normalize(name, NameMaxLength, "user.invalid-name", "A user name");
}

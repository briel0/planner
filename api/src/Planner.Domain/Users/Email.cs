namespace Planner.Domain.Users;

/// <summary>
/// Endereço de e-mail normalizado: sem espaços nas pontas, em minúsculas e com até 254 caracteres (limite do
/// padrão). A verificação de formato é propositalmente básica; a prova real de que o endereço existe é o envio
/// de uma confirmação, que pertence à autenticação.
/// </summary>
public sealed record Email {
    public const int MaxLength = 254;

    public Email(string value) {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Trim().ToLowerInvariant();
        if(normalized.Length > MaxLength || !HasBasicFormat(normalized)) {
            throw new DomainException("user.invalid-email", $"'{value}' is not a valid email address.");
        }
        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;

    /// <summary>Exatamente um '@', com algo antes, e um domínio com ponto; sem espaços.</summary>
    private static bool HasBasicFormat(string email) {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        if(at <= 0 || at != email.LastIndexOf('@') || email.Any(char.IsWhiteSpace)) {
            return false;
        }
        var domain = email[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        return dot > 0 && dot < domain.Length - 1;
    }
}

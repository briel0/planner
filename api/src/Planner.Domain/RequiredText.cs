namespace Planner.Domain;

/// <summary>Regra comum a títulos e nomes: sem espaços nas pontas, não vazio e com tamanho máximo.</summary>
internal static class RequiredText {
    public static string Normalize(string value, int maxLength, string errorCode, string description) {
        var trimmed = value.Trim();
        if(trimmed.Length == 0 || trimmed.Length > maxLength) {
            throw new DomainException(errorCode, $"{description} must have between 1 and {maxLength} characters.");
        }
        return trimmed;
    }
}

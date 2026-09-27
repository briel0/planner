namespace Planner.Domain;

/// <summary>
/// Violação de uma regra do domínio. O <see cref="Code"/> é estável e vira o campo <c>code</c> do
/// Problem Details na API (ver docs/api-design.md).
/// </summary>
public sealed class DomainException(string code, string message) : Exception(message) {
    public string Code { get; } = code;
}

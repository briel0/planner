namespace Planner.Application.Common;

public enum UseCaseErrorKind {
    /// <summary>O recurso não existe (ou não é do usuário: a resposta é a mesma, para não revelar que existe).</summary>
    NotFound,

    /// <summary>O pedido conflita com o estado atual (ex.: nome de categoria já usado).</summary>
    Conflict,

    /// <summary>O recurso mudou desde a versão informada em If-Match.</summary>
    StaleVersion,
}

/// <summary>
/// Um caso de uso não pôde ser concluído. O <see cref="Code"/> é estável e vira o campo <c>code</c> do Problem
/// Details; o <see cref="Kind"/> decide o status HTTP (404, 409, 412).
/// </summary>
public sealed class UseCaseException(UseCaseErrorKind kind, string code, string message) : Exception(message) {
    public UseCaseErrorKind Kind { get; } = kind;

    public string Code { get; } = code;

    public static UseCaseException NotFound(string what) => new(UseCaseErrorKind.NotFound, "not-found", $"{what} not found.");

    public static UseCaseException StaleVersion() =>
        new(UseCaseErrorKind.StaleVersion, "concurrency.stale", "The resource changed since the given version.");
}

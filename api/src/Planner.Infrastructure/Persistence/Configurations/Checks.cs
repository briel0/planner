namespace Planner.Infrastructure.Persistence.Configurations;

internal static class Checks {
    /// <summary>Texto obrigatório: sem espaços nas pontas e com 1 a <paramref name="maxLength"/> caracteres.</summary>
    public static string RequiredText(string column, int maxLength) =>
        $"{column} = btrim({column}) AND length({column}) BETWEEN 1 AND {maxLength}";
}

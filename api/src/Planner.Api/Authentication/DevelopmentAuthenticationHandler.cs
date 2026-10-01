using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Planner.Infrastructure.Development;

namespace Planner.Api.Authentication;

/// <summary>
/// Esquema de autenticação SÓ de desenvolvimento: todo pedido é autenticado como o usuário dev. Substituído pelo
/// login com o Google; o resto da API (claims, [Authorize], ICurrentUser) não muda.
/// </summary>
internal sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, DevelopmentUser.Id.ToString()),
                new Claim(ClaimTypes.Name, DevelopmentUser.Name),
                new Claim(ClaimTypes.Email, DevelopmentUser.Email),
            ],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new(identity), SchemeName)));
    }
}

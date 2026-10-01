using System.Security.Claims;
using Planner.Application.Common;

namespace Planner.Api.Authentication;

/// <summary>O usuário atual, lido das claims do pedido (vale para qualquer esquema de autenticação).</summary>
internal sealed class ClaimsCurrentUser(IHttpContextAccessor http) : ICurrentUser {
    public Guid Id =>
        Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("The request has no authenticated user.");
}

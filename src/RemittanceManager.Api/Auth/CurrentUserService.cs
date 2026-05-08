using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RemittanceManager.Application.Common.Exceptions;

namespace RemittanceManager.Api.Auth;

/// <summary>
/// Reads the authenticated user's id from the <c>sub</c> JWT claim.
/// Throws <see cref="UnauthorizedException"/> when the request is unauthenticated
/// or carries a malformed claim.
/// </summary>
public interface ICurrentUserService
{
    Guid GetUserId();
    Guid? TryGetUserId();
}

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor = accessor;

    public Guid GetUserId()
    {
        return TryGetUserId() ?? throw new UnauthorizedException("Authenticated user context is missing.");
    }

    public Guid? TryGetUserId()
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;

        var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}

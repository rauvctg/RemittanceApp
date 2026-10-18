using RemittanceManager.Application.Auth.Dtos;

namespace RemittanceManager.Application.Auth;

public interface IAuthService
{
    Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}

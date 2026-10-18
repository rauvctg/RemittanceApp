using RemittanceManager.Application.Abstractions.Security;
using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Application.Auth.Dtos;
using RemittanceManager.Application.Common.Exceptions;
using RemittanceManager.Application.Common.Constants;
using RemittanceManager.Domain.Entities;
using RemittanceManager.Domain.Repositories;

namespace RemittanceManager.Application.Auth;

public class AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator tokens, IClock clock) : IAuthService
{
    private readonly IUserRepository _users = users;
    private readonly IPasswordHasher _hasher = hasher;
    private readonly IJwtTokenGenerator _tokens = tokens;
    private readonly IClock _clock = clock;

    public async Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var displayName = (request.DisplayName ?? string.Empty).Trim();

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new ConflictException(string.Format(ValidationMessages.UserAlreadyExists, email));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAtUtc = _clock.UtcNow
        };

        await _users.AddAsync(user, ct);
        return user.Id;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(ValidationMessages.InvalidEmailOrPassword);

        var token = _tokens.Generate(user);
        return new AuthResponse { Token = token, UserId = user.Id, Email = user.Email, DisplayName = user.DisplayName };
    }

    private static string NormalizeEmail(string email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}

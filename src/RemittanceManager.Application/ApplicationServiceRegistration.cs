using Microsoft.Extensions.DependencyInjection;
using RemittanceManager.Application.Auth;
using RemittanceManager.Application.Remittances;

namespace RemittanceManager.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRemittanceService, RemittanceService>();
        return services;
    }
}

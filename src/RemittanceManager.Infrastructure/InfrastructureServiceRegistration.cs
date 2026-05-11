using RemittanceManager.Application.Common.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RemittanceManager.Application.Abstractions.Security;
using RemittanceManager.Application.Abstractions.Time;
using RemittanceManager.Domain.Repositories;
using RemittanceManager.Infrastructure.Persistence;
using RemittanceManager.Infrastructure.Persistence.Repositories;
using RemittanceManager.Infrastructure.Security;
using RemittanceManager.Infrastructure.Time;

namespace RemittanceManager.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public const string ConnectionStringName = AppConstants.ConnectionStringName;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Missing connection string '{ConnectionStringName}' in configuration.");

        services.Configure<JwtSettings>(configuration.GetSection(AppConstants.JwtSection));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRemittanceRepository, RemittanceRepository>();


        return services;
    }
}

    
namespace RemittanceManager.Application.Common.Constants;

public static class AppConstants
{
    public const string TrackingCodePrefix = "RM-";
    public const string CorsPolicy = "FrontendCors";
    public const string JwtSection = "Jwt";
    public const string ConnectionStringName = "DefaultConnection";
    public const string CorsAllowedOriginsSection = "Cors:AllowedOrigins";
    public const string DefaultFrontendOrigin = "http://localhost:5173";
    public const string SwaggerTitle = "RemittanceManager API";
    public const string SwaggerVersion = "v1";
    public const string SwaggerDescription = "Remittance tracking API. Use POST /api/auth/login to obtain a Bearer token.";
    public const string SwaggerAuthHeader = "Authorization";
    public const string SwaggerAuthDescription = "Enter your JWT in the format: Bearer {token}";
    public const string SwaggerBearerFormat = "JWT";
    public const string SwaggerUiTitle = "RemittanceManager API v1";
    public const string SwaggerUiRedirect = "/swagger/index.html";
}

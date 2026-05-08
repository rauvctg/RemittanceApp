using RemittanceManager.Application.Common.Constants;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RemittanceManager.Api.Auth;
using RemittanceManager.Api.Middleware;
using RemittanceManager.Application;
using RemittanceManager.Infrastructure;
using RemittanceManager.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = AppConstants.CorsPolicy;

builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication(); // ApplicationServiceRegistration.AddApplication
builder.Services.AddInfrastructure(builder.Configuration); // InfrastructureServiceRegistration.AddInfrastructure

// JWT Authentication
var jwtSection = builder.Configuration.GetSection(AppConstants.JwtSection);
var jwt = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException($"Missing '{AppConstants.JwtSection}' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// CORS - frontend origin from config
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection(AppConstants.CorsAllowedOriginsSection).Get<string[]>()
                  ?? new[] { AppConstants.DefaultFrontendOrigin };
    options.AddPolicy(CorsPolicy, p => p
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Swagger with Bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = AppConstants.SwaggerTitle,
        Version = AppConstants.SwaggerVersion,
        Description = AppConstants.SwaggerDescription
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = AppConstants.SwaggerAuthHeader,
        Description = AppConstants.SwaggerAuthDescription,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = AppConstants.SwaggerBearerFormat,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", bearerScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", AppConstants.SwaggerUiTitle);
        c.RoutePrefix = "swagger";
    });

     app.MapGet("/", () => Results.Redirect(AppConstants.SwaggerUiRedirect))
         .ExcludeFromDescription();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();

public partial class Program { }

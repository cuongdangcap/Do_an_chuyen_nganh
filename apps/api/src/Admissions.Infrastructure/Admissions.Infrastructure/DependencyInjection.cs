using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Admissions.Application.Auth;
using Admissions.Application.Admissions;
using Admissions.Application.Chat;
using Admissions.Application.Dashboard;
using Admissions.Application.Documents;
using Admissions.Application.Evaluation;
using Admissions.Application.Handoff;
using Admissions.Application.Profiles;
using Admissions.Application.Rag;
using Admissions.Application.Users;
using Admissions.Infrastructure.Options;
using Admissions.Infrastructure.Persistence;
using Admissions.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Admissions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = configuration["Jwt:Audience"] ?? string.Empty;
            options.Secret = configuration["Jwt:Secret"] ?? string.Empty;
            options.AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var accessMinutes)
                ? accessMinutes
                : options.AccessTokenMinutes;
            options.RefreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var refreshDays)
                ? refreshDays
                : options.RefreshTokenDays;
        });
        services.Configure<AiServiceOptions>(options =>
        {
            options.BaseUrl = configuration["AiService:BaseUrl"] ?? options.BaseUrl;
            options.TimeoutSeconds = int.TryParse(configuration["AiService:TimeoutSeconds"], out var timeoutSeconds)
                ? timeoutSeconds
                : options.TimeoutSeconds;
        });
        services.Configure<DocumentStorageOptions>(options =>
        {
            options.DocumentsPath = configuration["Storage:DocumentsPath"] ?? options.DocumentsPath;
            options.MaxFileSizeBytes = long.TryParse(configuration["Storage:MaxFileSizeBytes"], out var maxFileSizeBytes)
                ? maxFileSizeBytes
                : options.MaxFileSizeBytes;
        });
        services.Configure<LlmOptions>(options =>
        {
            options.Enabled = bool.TryParse(configuration["Llm:Enabled"], out var enabled) && enabled;
            options.BaseUrl = configuration["Llm:BaseUrl"] ?? options.BaseUrl;
            options.ApiKey = configuration["Llm:ApiKey"] ?? options.ApiKey;
            options.Model = configuration["Llm:Model"] ?? options.Model;
            options.TimeoutSeconds = int.TryParse(configuration["Llm:TimeoutSeconds"], out var timeoutSeconds)
                ? timeoutSeconds
                : options.TimeoutSeconds;
        });

        services.AddDbContext<AdmissionsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpClient<DocumentIngestionClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<LlmAnswerService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<LlmOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IAdmissionsService, AdmissionsService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<IEvaluationService, EvaluationService>();
        services.AddScoped<IHandoffService, HandoffService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

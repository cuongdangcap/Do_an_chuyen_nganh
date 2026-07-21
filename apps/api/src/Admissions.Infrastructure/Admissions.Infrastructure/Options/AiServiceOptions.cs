namespace Admissions.Infrastructure.Options;

public sealed class AiServiceOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 120;
}

using Admissions.Api.Common;
using Admissions.Infrastructure.Options;
using Admissions.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Admissions.Api.Controllers;

[ApiController]
[Route("api/admin/ai/status")]
[Authorize(Roles = "admin,staff")]
public sealed class AdminAiStatusController(
    DocumentIngestionClient ingestionClient,
    IOptions<LlmOptions> llmOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var llm = llmOptions.Value;
        AiHealthResponse? aiHealth = null;
        string aiStatus = "unavailable";
        try
        {
            aiHealth = await ingestionClient.GetHealthAsync(cancellationToken);
            aiStatus = aiHealth?.Success == true ? "ok" : "unavailable";
        }
        catch
        {
            aiStatus = "unavailable";
        }

        var response = new AdminAiStatusResponse(
            aiStatus,
            aiHealth?.Vector?.Backend ?? "unknown",
            aiHealth?.Vector?.QdrantAvailable ?? false,
            aiHealth?.Vector?.QdrantUrl,
            llm.Enabled,
            llm.Enabled
                && !string.IsNullOrWhiteSpace(llm.BaseUrl)
                && !string.IsNullOrWhiteSpace(llm.ApiKey)
                && !string.IsNullOrWhiteSpace(llm.Model),
            MaskBaseUrl(llm.BaseUrl),
            llm.Model);

        return Ok(ApiResponse<AdminAiStatusResponse>.Ok(response, "AI status loaded.", HttpContext.TraceIdentifier));
    }

    private static string? MaskBaseUrl(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/');
    }
}

public sealed record AdminAiStatusResponse(
    string AiServiceStatus,
    string VectorBackend,
    bool QdrantAvailable,
    string? QdrantUrl,
    bool LlmEnabled,
    bool LlmConfigured,
    string? LlmBaseUrl,
    string LlmModel);

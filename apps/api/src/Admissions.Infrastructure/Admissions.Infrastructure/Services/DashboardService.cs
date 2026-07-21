using Admissions.Application.Dashboard;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class DashboardService(AdmissionsDbContext dbContext) : IDashboardService
{
    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken)
    {
        var latestEvaluation = await dbContext.EvaluationRuns
            .Where(x => x.Status != "running" && x.Results.Any())
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var latencyValues = await dbContext.ChatMessages
            .Where(x => x.Role == "assistant" && x.LatencyMs != null)
            .Select(x => x.LatencyMs!.Value)
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto(
            await dbContext.Users.CountAsync(cancellationToken),
            await dbContext.KnowledgeDocuments.CountAsync(cancellationToken),
            await dbContext.DocumentVersions.CountAsync(x => x.ProcessingStatus == "completed", cancellationToken),
            await dbContext.ChatConversations.CountAsync(cancellationToken),
            await dbContext.ChatMessages.CountAsync(cancellationToken),
            await dbContext.ChatFeedback.CountAsync(x => x.Rating == "negative", cancellationToken),
            await dbContext.HandoffTickets.CountAsync(x => x.Status == "open" || x.Status == "in_progress", cancellationToken),
            await dbContext.HandoffTickets.CountAsync(x => x.Status == "resolved" || x.Status == "closed", cancellationToken),
            await dbContext.EvaluationRuns.CountAsync(cancellationToken),
            latestEvaluation?.HitRateAtK ?? 0,
            latestEvaluation?.AverageKeywordHitRate ?? 0,
            latencyValues.Count == 0 ? 0 : latencyValues.Average());
    }
}

namespace Admissions.Application.Dashboard;

public sealed record AdminDashboardDto(
    int TotalUsers,
    int TotalDocuments,
    int CompletedDocumentVersions,
    int TotalConversations,
    int TotalChatMessages,
    int NegativeFeedback,
    int OpenHandoffTickets,
    int ResolvedHandoffTickets,
    int EvaluationRuns,
    double LatestEvaluationHitRateAtK,
    double LatestEvaluationKeywordHitRate,
    double AverageChatLatencyMs);

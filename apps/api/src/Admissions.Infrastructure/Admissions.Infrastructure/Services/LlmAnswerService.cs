using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Admissions.Application.Rag;
using Admissions.Infrastructure.Options;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Admissions.Infrastructure.Services;

public sealed class LlmAnswerService(
    HttpClient httpClient,
    IOptions<LlmOptions> options,
    AdmissionsDbContext dbContext,
    ConversationMemoryContext memoryContext)
{
    private readonly LlmOptions _options = options.Value;

    public bool IsEnabled =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.BaseUrl)
        && !string.IsNullOrWhiteSpace(_options.ApiKey)
        && !string.IsNullOrWhiteSpace(_options.Model);

    public async Task<string?> TryGenerateAsync(string question, IReadOnlyCollection<RagSearchResult> sources, CancellationToken cancellationToken)
    {
        if (!IsEnabled || sources.Count == 0)
        {
            return null;
        }

        var context = string.Join(
            "\n\n",
            sources.Select((source, index) =>
                $"[Nguồn {index + 1}: {source.Title ?? "Tài liệu"}, điểm={source.Score:F3}]\n{source.Content}"));
        var conversationMemory = await BuildConversationMemoryAsync(cancellationToken);
        var memoryBlock = string.IsNullOrWhiteSpace(conversationMemory)
            ? string.Empty
            : $"Ngữ cảnh hội thoại trước đó (chỉ dùng để hiểu đại từ/câu hỏi nối tiếp, không dùng làm nguồn sự thật):\n{conversationMemory}\n\n";

        var request = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessagePayload(
                    "system",
                    "Bạn là trợ lý tư vấn tuyển sinh đại học. Chỉ trả lời dữ kiện dựa trên các nguồn được cung cấp. " +
                    "Bạn có thể dùng lịch sử hội thoại để hiểu người dùng đang nói tới ngành, năm học hoặc chủ đề nào, nhưng lịch sử không được dùng để bịa hay thay thế nguồn tài liệu. " +
                    "Luôn trả lời bằng tiếng Việt có dấu, rõ ràng và ngắn gọn. " +
                    "Chỉ trả lời đúng phạm vi câu hỏi; không tự bổ sung học phí, hồ sơ, phương thức xét tuyển hoặc thông tin khác nếu người dùng không hỏi. " +
                    "Ưu tiên câu trả lời trực tiếp trước, sau đó mới thêm ghi chú thật sự cần thiết. " +
                    "Nếu nguồn không đủ dữ liệu, hãy nói rõ là chưa đủ dữ liệu. " +
                    "Khi dùng thông tin từ nguồn, gắn nhãn trích dẫn như [Nguồn 1], [Nguồn 2]."),
                new ChatMessagePayload(
                    "user",
                    $"{memoryBlock}Câu hỏi hiện tại: {question}\n\nNguồn tài liệu:\n{context}")
            ],
            0.2);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(request),
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
            var answer = payload?.Choices.FirstOrDefault()?.Message.Content?.Trim();
            return ContainsVietnameseAccent(answer) ? answer : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> BuildConversationMemoryAsync(CancellationToken cancellationToken)
    {
        if (memoryContext.ConversationId is not { } conversationId)
        {
            return null;
        }

        var trimmedSessionId = string.IsNullOrWhiteSpace(memoryContext.ClientSessionId)
            ? null
            : memoryContext.ClientSessionId.Trim();
        var allowed = memoryContext.UserId is { } userId
            ? await dbContext.ChatConversations.AnyAsync(
                x => x.Id == conversationId && x.UserId == userId,
                cancellationToken)
            : trimmedSessionId is not null
                && await dbContext.ChatConversations.AnyAsync(
                    x => x.Id == conversationId && x.UserId == null && x.ClientSessionId == trimmedSessionId,
                    cancellationToken);

        if (!allowed)
        {
            return null;
        }

        var recent = await dbContext.ChatMessages
            .Where(x => x.ConversationId == conversationId && (x.Role == "user" || x.Role == "assistant"))
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new { x.Role, x.Content, x.CreatedAt })
            .ToListAsync(cancellationToken);

        if (recent.Count == 0)
        {
            return null;
        }

        return string.Join(
            "\n",
            recent
                .OrderBy(x => x.CreatedAt)
                .Select(message =>
                {
                    var content = message.Content.ReplaceLineEndings(" ").Trim();
                    if (content.Length > 600)
                    {
                        content = content[..600] + "...";
                    }

                    return $"{(message.Role == "user" ? "Người dùng" : "Trợ lý")}: {content}";
                }));
    }

    private static bool ContainsVietnameseAccent(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Any(ch => "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐ".Contains(ch));
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyCollection<ChatMessagePayload> Messages,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record ChatMessagePayload(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyCollection<ChatChoice> Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessagePayload Message);
}
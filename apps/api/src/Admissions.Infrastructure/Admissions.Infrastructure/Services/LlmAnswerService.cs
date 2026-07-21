using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Admissions.Application.Rag;
using Admissions.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Admissions.Infrastructure.Services;

public sealed class LlmAnswerService(HttpClient httpClient, IOptions<LlmOptions> options)
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

        var request = new ChatCompletionRequest(
            _options.Model,
            [
                new ChatMessagePayload(
                    "system",
                    "Bạn là trợ lý tư vấn tuyển sinh đại học. Chỉ trả lời dựa trên các nguồn được cung cấp. Luôn trả lời bằng tiếng Việt có dấu, rõ ràng, tự nhiên và ngắn gọn. Nếu nguồn không đủ dữ liệu, hãy nói rõ là chưa đủ dữ liệu. Khi dùng thông tin từ nguồn, gắn nhãn trích dẫn như [Nguồn 1], [Nguồn 2]."),
                new ChatMessagePayload(
                    "user",
                    $"Câu hỏi: {question}\n\nNguồn tài liệu:\n{context}")
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

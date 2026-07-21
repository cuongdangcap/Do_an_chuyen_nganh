using Admissions.Application.Chat;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Admissions.Infrastructure.Services;

public sealed class ChatHistoryService(
    AdmissionsDbContext dbContext,
    DocumentIngestionClient ingestionClient) : IChatHistoryService
{
    public async Task<ChatConversationListResponse> ListAsync(Guid? userId, string? clientSessionId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = ApplyAccessFilter(dbContext.ChatConversations.Include(x => x.Messages).AsQueryable(), userId, clientSessionId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ChatConversationListResponse(items.Select(ToSummaryDto).ToList(), total);
    }

    public async Task<ChatConversationDetailDto?> GetAsync(Guid id, Guid? userId, string? clientSessionId, CancellationToken cancellationToken)
    {
        var query = ApplyAccessFilter(
            dbContext.ChatConversations
                .Include(x => x.Messages)
                .ThenInclude(x => x.Sources)
                .AsQueryable(),
            userId,
            clientSessionId);
        var conversation = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return conversation is null ? null : ToDetailDto(conversation);
    }

    public async Task<ChatConversationDetailDto> AskFileAsync(ChatFileQuestionCommand command, CancellationToken cancellationToken)
    {
        if (command.FileSizeBytes <= 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (command.FileSizeBytes > 15 * 1024 * 1024)
        {
            throw new InvalidOperationException("Uploaded chat file is too large.");
        }

        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var ingestion = await ingestionClient.ProcessFileAsync(
            command.Content,
            Path.GetFileName(command.FileName),
            command.ContentType,
            documentId,
            versionId,
            $"Chat upload - {command.FileName}",
            "chat_upload",
            cancellationToken);

        var conversation = await ResolveConversationAsync(command, cancellationToken);
        var now = DateTime.UtcNow;
        var userContent = $"{command.Question.Trim()}\n\n[File: {Path.GetFileName(command.FileName)}]";
        var answer = BuildFileAnswer(command.Question, command.FileName, ingestion.Chunks);

        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            UserId = command.UserId,
            Role = "user",
            Content = userContent,
            CreatedAt = now,
        };
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = answer,
            RetrievalBackend = "chat_upload",
            LatencyMs = null,
            CreatedAt = now.AddMilliseconds(1),
            Sources = ingestion.Chunks.Take(5).Select(chunk => new ChatMessageSource
            {
                PointId = chunk.PointId,
                Score = 1,
                Content = chunk.Content,
                Title = Path.GetFileName(command.FileName),
                DocumentType = "chat_upload",
                PageNumber = chunk.PageNumber,
                SectionTitle = chunk.SectionTitle,
            }).ToList(),
        };

        conversation.UpdatedAt = now;
        dbContext.ChatMessages.Add(userMessage);
        dbContext.ChatMessages.Add(assistantMessage);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(conversation.Id, command.UserId, command.ClientSessionId, cancellationToken)
            ?? throw new InvalidOperationException("Chat upload conversation was not saved.");
    }

    private static IQueryable<ChatConversation> ApplyAccessFilter(IQueryable<ChatConversation> query, Guid? userId, string? clientSessionId)
    {
        if (userId is not null)
        {
            return query.Where(x => x.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(clientSessionId))
        {
            var session = clientSessionId.Trim();
            return query.Where(x => x.UserId == null && x.ClientSessionId == session);
        }

        return query.Where(_ => false);
    }

    private static ChatConversationSummaryDto ToSummaryDto(ChatConversation conversation)
    {
        var last = conversation.Messages.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return new ChatConversationSummaryDto(
            conversation.Id,
            conversation.Title,
            conversation.Status,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            last is null ? null : Trim(last.Content.ReplaceLineEndings(" "), 120));
    }

    private static ChatConversationDetailDto ToDetailDto(ChatConversation conversation)
    {
        return new ChatConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.Status,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages.OrderBy(x => x.CreatedAt).Select(ToMessageDto).ToList());
    }

    private static ChatMessageDto ToMessageDto(ChatMessage message)
    {
        return new ChatMessageDto(
            message.Id,
            message.Role,
            message.Content,
            message.RetrievalBackend,
            message.LatencyMs,
            message.CreatedAt,
            message.Sources.OrderByDescending(x => x.Score).Select(ToSourceDto).ToList());
    }

    private static ChatMessageSourceDto ToSourceDto(ChatMessageSource source)
    {
        return new ChatMessageSourceDto(
            source.Id,
            source.PointId,
            source.Score,
            source.Content,
            source.Title,
            source.DocumentType,
            source.PageNumber,
            source.SectionTitle);
    }

    private static string Trim(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private async Task<ChatConversation> ResolveConversationAsync(ChatFileQuestionCommand command, CancellationToken cancellationToken)
    {
        if (command.ConversationId is { } conversationId)
        {
            var existing = await ApplyAccessFilter(dbContext.ChatConversations.AsQueryable(), command.UserId, command.ClientSessionId)
                .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        var title = command.Question.ReplaceLineEndings(" ").Trim();
        var conversation = new ChatConversation
        {
            UserId = command.UserId,
            ClientSessionId = string.IsNullOrWhiteSpace(command.ClientSessionId) ? null : command.ClientSessionId.Trim(),
            Title = title.Length <= 80 ? title : title[..80] + "...",
        };
        dbContext.ChatConversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private static string BuildFileAnswer(string question, string fileName, IReadOnlyCollection<AiChunkResponse> chunks)
    {
        var lines = new List<string>
        {
            $"Dựa trên tệp bạn vừa tải lên ({Path.GetFileName(fileName)}), hệ thống tìm thấy các đoạn liên quan sau:",
        };
        foreach (var chunk in chunks.Take(4))
        {
            lines.Add($"- {Trim(chunk.Content.ReplaceLineEndings(" "), 260)}");
        }

        lines.Add($"Câu hỏi của bạn: {question.Trim()}");
        return string.Join(Environment.NewLine, lines);
    }
}

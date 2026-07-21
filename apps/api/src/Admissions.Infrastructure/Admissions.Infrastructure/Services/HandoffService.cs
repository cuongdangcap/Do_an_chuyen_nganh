using Admissions.Application.Handoff;
using Admissions.Domain.Entities;
using Admissions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Services;

public sealed class HandoffService(
    AdmissionsDbContext dbContext,
    IHandoffRealtimeNotifier realtimeNotifier) : IHandoffService
{
    public async Task<HandoffTicketDto> CreateFromAssistantMessageAsync(CreateHandoffTicketRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var message = await LoadAssistantMessageAsync(request.AssistantMessageId, cancellationToken);
        var existing = await FindExistingOpenTicketAsync(message.Id, cancellationToken);
        if (existing is not null)
        {
            return ToTicketDto(existing);
        }

        var ticket = BuildTicket(
            message,
            feedback: null,
            userId,
            string.IsNullOrWhiteSpace(request.Reason) ? "manual_request" : request.Reason.Trim(),
            request.Note);
        dbContext.HandoffTickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = await GetAsync(ticket.Id, cancellationToken) ?? ToTicketDto(ticket);
        await realtimeNotifier.TicketCreatedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<HandoffTicketDto> CreateFromNegativeFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var feedback = await dbContext.ChatFeedback
            .Include(x => x.Message)
            .ThenInclude(x => x.Conversation)
            .ThenInclude(x => x.Messages)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == feedbackId, cancellationToken)
            ?? throw new KeyNotFoundException("Feedback not found.");

        if (!string.Equals(feedback.Rating, "negative", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only negative feedback creates handoff tickets.");
        }

        var existing = await FindExistingOpenTicketAsync(feedback.MessageId, cancellationToken);
        if (existing is not null)
        {
            return ToTicketDto(existing);
        }

        var ticket = BuildTicket(feedback.Message, feedback, feedback.UserId, "negative_feedback", feedback.Note);
        dbContext.HandoffTickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = await GetAsync(ticket.Id, cancellationToken) ?? ToTicketDto(ticket);
        await realtimeNotifier.TicketCreatedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<HandoffTicketListResponse> ListAsync(string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = BaseTicketQuery();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = NormalizeStatus(status);
            query = query.Where(x => x.Status == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var tickets = await query
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new HandoffTicketListResponse(tickets.Select(ToTicketDto).ToList(), total);
    }

    public async Task<HandoffTicketDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await BaseTicketQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return ticket is null ? null : ToTicketDto(ticket);
    }

    public async Task<HandoffTicketDto> ReplyAsync(Guid ticketId, ReplyHandoffTicketRequest request, Guid staffUserId, CancellationToken cancellationToken)
    {
        var ticket = await BaseTicketQuery()
            .FirstOrDefaultAsync(x => x.Id == ticketId, cancellationToken)
            ?? throw new KeyNotFoundException("Handoff ticket not found.");

        var content = RequireContent(request.Content);
        var now = DateTime.UtcNow;
        ticket.Status = request.Resolve ? "resolved" : "in_progress";
        ticket.AssignedToUserId ??= staffUserId;
        ticket.StaffReplyPreview = content.Length <= 1000 ? content : content[..1000];
        ticket.UpdatedAt = now;
        ticket.ResolvedAt = request.Resolve ? now : ticket.ResolvedAt;
        dbContext.HandoffMessages.Add(new HandoffMessage
        {
            TicketId = ticket.Id,
            SenderUserId = staffUserId,
            SenderRole = "staff",
            Content = content,
            CreatedAt = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = await GetAsync(ticket.Id, cancellationToken) ?? ToTicketDto(ticket);
        await realtimeNotifier.TicketUpdatedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<HandoffTicketDto> UpdateStatusAsync(Guid ticketId, UpdateHandoffTicketStatusRequest request, Guid staffUserId, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.HandoffTickets
            .FirstOrDefaultAsync(x => x.Id == ticketId, cancellationToken)
            ?? throw new KeyNotFoundException("Handoff ticket not found.");

        var status = NormalizeStatus(request.Status);
        ticket.Status = status;
        ticket.AssignedToUserId ??= staffUserId;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.ResolvedAt = status is "resolved" or "closed" ? DateTime.UtcNow : null;
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = await GetAsync(ticket.Id, cancellationToken) ?? ToTicketDto(ticket);
        await realtimeNotifier.TicketUpdatedAsync(dto, cancellationToken);
        return dto;
    }

    private IQueryable<HandoffTicket> BaseTicketQuery()
    {
        return dbContext.HandoffTickets
            .Include(x => x.CreatedByUser)
            .Include(x => x.AssignedToUser)
            .Include(x => x.Messages)
            .ThenInclude(x => x.SenderUser);
    }

    private async Task<ChatMessage> LoadAssistantMessageAsync(Guid assistantMessageId, CancellationToken cancellationToken)
    {
        return await dbContext.ChatMessages
            .Include(x => x.Conversation)
            .ThenInclude(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == assistantMessageId && x.Role == "assistant", cancellationToken)
            ?? throw new KeyNotFoundException("Assistant message not found.");
    }

    private async Task<HandoffTicket?> FindExistingOpenTicketAsync(Guid sourceMessageId, CancellationToken cancellationToken)
    {
        return await BaseTicketQuery()
            .FirstOrDefaultAsync(
                x => x.SourceMessageId == sourceMessageId && x.Status != "resolved" && x.Status != "closed",
                cancellationToken);
    }

    private static HandoffTicket BuildTicket(ChatMessage message, ChatFeedback? feedback, Guid? userId, string reason, string? note)
    {
        var question = FindQuestion(message);
        var ticket = new HandoffTicket
        {
            ConversationId = message.ConversationId,
            SourceMessageId = message.Id,
            FeedbackId = feedback?.Id,
            CreatedByUserId = userId,
            Status = "open",
            Priority = reason == "negative_feedback" ? "high" : "normal",
            Reason = reason,
            Question = question,
            AiAnswer = message.Content,
        };

        if (!string.IsNullOrWhiteSpace(note))
        {
            ticket.Messages.Add(new HandoffMessage
            {
                TicketId = ticket.Id,
                SenderUserId = userId,
                SenderRole = userId is null ? "guest" : "user",
                Content = note.Trim(),
            });
        }

        return ticket;
    }

    private static string FindQuestion(ChatMessage assistantMessage)
    {
        return assistantMessage.Conversation.Messages
            .Where(x => x.Role == "user" && x.CreatedAt <= assistantMessage.CreatedAt)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Content)
            .FirstOrDefault() ?? string.Empty;
    }

    private static string NormalizeStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "open" or "in_progress" or "resolved" or "closed" => normalized,
            _ => throw new InvalidOperationException("Status must be open, in_progress, resolved, or closed."),
        };
    }

    private static string RequireContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Reply content is required.");
        }

        return content.Trim();
    }

    private static HandoffTicketDto ToTicketDto(HandoffTicket ticket)
    {
        return new HandoffTicketDto(
            ticket.Id,
            ticket.ConversationId,
            ticket.SourceMessageId,
            ticket.FeedbackId,
            ticket.CreatedByUserId,
            ticket.CreatedByUser?.Email,
            ticket.AssignedToUserId,
            ticket.AssignedToUser?.Email,
            ticket.Status,
            ticket.Priority,
            ticket.Reason,
            ticket.Question,
            ticket.AiAnswer,
            ticket.StaffReplyPreview,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.Messages.OrderBy(x => x.CreatedAt).Select(ToMessageDto).ToList());
    }

    private static HandoffMessageDto ToMessageDto(HandoffMessage message)
    {
        return new HandoffMessageDto(
            message.Id,
            message.TicketId,
            message.SenderUserId,
            message.SenderUser?.Email,
            message.SenderRole,
            message.Content,
            message.CreatedAt);
    }
}

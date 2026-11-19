using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Entities;

/// Append-only (BR-16): مفيش ميثود تعدّل، والـ constructor internal.
public class TicketHistory
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid ChangedById { get; private set; }
    public TicketHistoryAction Action { get; private set; }
    public TicketStatus? FromStatus { get; private set; }
    public TicketStatus? ToStatus { get; private set; }
    public DateTime? PreviousDeadline { get; private set; }   // BR-19
    public Guid? AssigneeId { get; private set; }             // BR-15 للتعيين
    public DateTime ChangedAt { get; private set; }

    private TicketHistory() { }   // For EF Core only

    internal TicketHistory(
        Guid ticketId, Guid changedById, TicketHistoryAction action,
        TicketStatus? from, TicketStatus? to,
        DateTime? previousDeadline, Guid? assigneeId, DateTime changedAt)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        ChangedById = changedById;
        Action = action;
        FromStatus = from;
        ToStatus = to;
        PreviousDeadline = previousDeadline;
        AssigneeId = assigneeId;
        ChangedAt = changedAt;
    }
}
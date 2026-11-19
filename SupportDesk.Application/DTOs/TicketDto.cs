using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;

public record TicketDto(
    Guid Id, string Title, string Description,
    TicketStatus Status, TicketPriority Priority,               
    Guid CustomerId, Guid? AgentId, Guid CategoryId,
    DateTime CreatedAt, DateTime ResolutionDeadline,
    DateTime? ResolvedAt, DateTime? ClosedAt)
{
    public static TicketDto From(Ticket t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.CustomerId, t.AgentId, t.CategoryId,
        t.CreatedAt, t.ResolutionDeadline, t.ResolvedAt, t.ClosedAt);
}
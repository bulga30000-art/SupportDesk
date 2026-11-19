using SupportDesk.Domain.Exceptions;

namespace SupportDesk.Domain.Entities;

public class TicketComment
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private TicketComment() { }   // For EF Core only

    // internal: الـ Ticket بس هي اللي تقدر تعمل تعليق
    internal TicketComment(Guid ticketId, Guid authorId, string body, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Comment body is required.");

        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorId = authorId;
        Body = body.Trim();
        CreatedAt = createdAt;
    }
}
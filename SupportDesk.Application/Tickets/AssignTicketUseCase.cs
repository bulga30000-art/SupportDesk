using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.ValueObjects;

public class AssignTicketUseCase
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public AssignTicketUseCase(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<TicketDto> ExecuteAsync(
        Actor actor, Guid ticketId, Actor assignee, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket not found.");

        ticket.Assign(actor, assignee, _clock);
        await _db.SaveChangesAsync(ct);

        return TicketDto.From(ticket);
    }
}
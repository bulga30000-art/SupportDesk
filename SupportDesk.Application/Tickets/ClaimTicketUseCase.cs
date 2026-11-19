// Tickets/ClaimTicketUseCase.cs
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.ValueObjects;

public class ClaimTicketUseCase
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public ClaimTicketUseCase(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<TicketDto> ExecuteAsync(Actor actor, Guid ticketId, CancellationToken ct)
    {
        // مقصود: من غير Include للـ History أو Comments، مش محتاجينهم هنا
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new NotFoundException("Ticket not found.");

        ticket.Claim(actor, _clock);          // الـ Domain يفرض القواعد ويكتب الـ History
        await _db.SaveChangesAsync(ct);

        return TicketDto.From(ticket);
    }
}
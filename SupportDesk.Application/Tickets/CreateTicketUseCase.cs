using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Options;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.ValueObjects;

public class CreateTicketUseCase
{
    private readonly IApplicationDbContext _db;
    private readonly SlaOptions _sla;
    private readonly TimeProvider _clock;

    public CreateTicketUseCase(IApplicationDbContext db, IOptions<SlaOptions> sla, TimeProvider clock)
    {
        _db = db;
        _sla = sla.Value;
        _clock = clock;
    }

    // BR-03 (Customer فقط) بيتفرض في المرحلة 5
    public async Task<TicketDto> ExecuteAsync(
        Actor customer, string title, string description,
        Guid categoryId, TicketPriority priority, CancellationToken ct)
    {
        if (!Enum.IsDefined(priority))   // مؤقت: هيروح لـ FluentValidation في المرحلة 4
            throw new DomainException("Priority is invalid.");

        var ticket = new Ticket(title, description, categoryId, customer.Id,
                                priority, _sla.For(priority), _clock);

        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            throw new NotFoundException("Category not found.");

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        return TicketDto.From(ticket);
    }
}
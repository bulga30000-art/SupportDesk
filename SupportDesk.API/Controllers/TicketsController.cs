// Controllers/TicketsController.cs
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.ValueObjects;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly CreateTicketUseCase _create;
    private readonly ClaimTicketUseCase _claim;
    private readonly AssignTicketUseCase _assign;

    public TicketsController(CreateTicketUseCase create, ClaimTicketUseCase claim, AssignTicketUseCase assign)
    {
        _create = create;
        _claim = claim;
        _assign = assign;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketRequest request, CancellationToken ct)
    {
        var dto = await _create.ExecuteAsync(TempActor.From(Request),
            request.Title, request.Description, request.CategoryId, request.Priority, ct);
        return Created($"/api/tickets/{dto.Id}", dto);
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> Claim(Guid id, CancellationToken ct) =>
        Ok(await _claim.ExecuteAsync(TempActor.From(Request), id, ct));

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignTicketRequest request, CancellationToken ct) =>
        Ok(await _assign.ExecuteAsync(TempActor.From(Request), id,
            new Actor(request.AgentId, request.AgentRole), ct));
}

public record CreateTicketRequest(string Title, string Description, Guid CategoryId, TicketPriority Priority);

// مؤقت: الدور المفروض يجي من جدول المستخدمين (المرحلة 5)
public record AssignTicketRequest(Guid AgentId, UserRole AgentRole);
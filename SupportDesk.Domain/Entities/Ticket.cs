using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.ValueObjects;

namespace SupportDesk.Domain.Entities;

public class Ticket
{
    // الصناديق الخاصة: بس الـ Ticket تقدر تضيف فيها (Backing Fields)
    private readonly List<TicketComment> _comments = new();
    private readonly List<TicketHistory> _history = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }

    public Guid CustomerId { get; private set; }
    public Guid? AgentId { get; private set; }
    public Guid CategoryId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime ResolutionDeadline { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // بيتحدّث تلقائيًا من SQL Server مع كل تعديل (Optimistic Concurrency)
    //مفيش ميثود تغيّره. الداتابيز هي اللي بتتحكم فيه، والـ Domain بيحمله بس.
    public byte[] RowVersion { get; private set; } = null!;

    // نوافذ قراءة فقط
    public IReadOnlyCollection<TicketComment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<TicketHistory> History => _history.AsReadOnly();

    // For EF Core only
    private Ticket() { }

    public Ticket(
        string title,
        string description,
        Guid categoryId,
        Guid customerId,
        TicketPriority priority,
        TimeSpan resolutionTime,
        TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");
        if (!Enum.IsDefined(priority))
            throw new DomainException("Priority is invalid.");
        if (resolutionTime <= TimeSpan.Zero)
            throw new DomainException("Resolution time must be positive.");

        var now = clock.GetUtcNow().UtcDateTime;

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description.Trim();
        CategoryId = categoryId;
        CustomerId = customerId;
        Priority = priority;
        Status = TicketStatus.Open;                  // BR-02
        AgentId = null;                              // BR-02
        CreatedAt = now;
        ResolutionDeadline = now + resolutionTime;   // BR-18

        RecordHistory(customerId, TicketHistoryAction.Created, now, to: TicketStatus.Open);
    }

    // BR-17: بتتحسب، مش بتتخزن
    public bool IsOverdue(TimeProvider clock) =>
        Status is TicketStatus.Open or TicketStatus.InProgress
        && clock.GetUtcNow().UtcDateTime > ResolutionDeadline;

    // ---------- State machine (قسم 3 في الـ requirements) ----------
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Open] = [TicketStatus.InProgress],
        [TicketStatus.InProgress] = [TicketStatus.Resolved],
        [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.Open],
        [TicketStatus.Closed] = [TicketStatus.Open],
    };

    private void MoveTo(TicketStatus next)
    {
        if (!AllowedTransitions[Status].Contains(next))   // BR-11
            throw new DomainException($"Cannot move a ticket from {Status} to {next}.");

        Status = next;
    }

    private bool IsOwner(Actor actor) =>
        actor.Role == UserRole.Customer && actor.Id == CustomerId;

    private bool IsAssignedAgent(Actor actor) =>
        actor.Role == UserRole.SupportAgent && AgentId == actor.Id;

    private void EnsureOpen()
    {
        if (Status != TicketStatus.Open)                  // BR-04
            throw new DomainException("Only an open ticket can be assigned or claimed.");
    }

    // الطريق الوحيد لإضافة صف History (BR-15)
    private void RecordHistory(
        Guid changedById, TicketHistoryAction action, DateTime at,
        TicketStatus? from = null, TicketStatus? to = null,
        DateTime? previousDeadline = null, Guid? assigneeId = null)
    {
        _history.Add(new TicketHistory(
            Id, changedById, action, from, to, previousDeadline, assigneeId, at));
    }

    // ---------- Assignment (مش بتغيّر الحالة) ----------
    public void Assign(Actor actor/*الادمن الي هيعين التذكرة لموظف الدعم*/, Actor assignee/*موظف الدعم الي هياخد التذكرة*/, TimeProvider clock)
    {
        if (actor.Role != UserRole.Admin)
            throw new ForbiddenActionException("Only an admin can assign a ticket.");
        if (assignee.Role != UserRole.SupportAgent)      // BR-05
            throw new DomainException("A ticket can only be assigned to a support agent.");
        EnsureOpen();

        AgentId = assignee.Id;
        RecordHistory(actor.Id, TicketHistoryAction.Assigned,
            clock.GetUtcNow().UtcDateTime, assigneeId: assignee.Id);
    }

    public void Claim(Actor actor /*موظف الدعم الي هياخد التذكرة*/, TimeProvider clock)
    {
        if (actor.Role != UserRole.SupportAgent)         // BR-06
            throw new ForbiddenActionException("Only a support agent can claim a ticket.");
        EnsureOpen();
        if (AgentId is not null)                         // BR-06
            throw new DomainException("This ticket is already assigned.");

        AgentId = actor.Id;
        RecordHistory(actor.Id, TicketHistoryAction.Claimed,
            clock.GetUtcNow().UtcDateTime, assigneeId: actor.Id);
    }

    // ---------- Lifecycle ----------
    public void Start(Actor actor, TimeProvider clock)
    {
        if (!IsAssignedAgent(actor))                     // BR-08
            throw new ForbiddenActionException("Only the assigned agent can start this ticket.");

        var from = Status;
        MoveTo(TicketStatus.InProgress);
        RecordHistory(actor.Id, TicketHistoryAction.Started,
            clock.GetUtcNow().UtcDateTime, from, Status);
    }

    public void Resolve(Actor actor, TimeProvider clock)
    {
        if (!IsAssignedAgent(actor))                     // BR-09
            throw new ForbiddenActionException("Only the assigned agent can resolve this ticket.");

        var now = clock.GetUtcNow().UtcDateTime;
        var from = Status;
        MoveTo(TicketStatus.Resolved);
        ResolvedAt = now;
        RecordHistory(actor.Id, TicketHistoryAction.Resolved, now, from, Status);
    }

    public void Close(Actor actor, TimeProvider clock)
    {
        if (actor.Role != UserRole.Admin && !IsOwner(actor))   // BR-10
            throw new ForbiddenActionException("Only the ticket owner or an admin can close this ticket.");

        var now = clock.GetUtcNow().UtcDateTime;
        var from = Status;
        MoveTo(TicketStatus.Closed);
        ClosedAt = now;
        RecordHistory(actor.Id, TicketHistoryAction.Closed, now, from, Status);
    }

    public void Reopen(Actor actor, TimeSpan resolutionTime, TimeProvider clock)
    {
        if (resolutionTime <= TimeSpan.Zero)
            throw new DomainException("Resolution time must be positive.");

        var allowed = Status == TicketStatus.Closed
            ? actor.Role == UserRole.Admin                         // BR-12
            : actor.Role == UserRole.Admin || IsOwner(actor);      // Resolved -> Open
        if (!allowed)
            throw new ForbiddenActionException("You are not allowed to reopen this ticket.");

        var now = clock.GetUtcNow().UtcDateTime;
        var from = Status;
        var previousDeadline = ResolutionDeadline;

        MoveTo(TicketStatus.Open);                       // كل التحققات قبل أي تعديل

        AgentId = null;                                  // BR-12
        ResolvedAt = null;
        ClosedAt = null;
        ResolutionDeadline = now + resolutionTime;       // BR-19

        RecordHistory(actor.Id, TicketHistoryAction.Reopened, now, from, Status,
            previousDeadline: previousDeadline);         // BR-19
    }

    // ---------- Comments ----------
    // BR-14 (مين يعلّق) في المرحلة 5
    public TicketComment AddComment(Actor author, string body, TimeProvider clock)
    {
        if (Status == TicketStatus.Closed)                // BR-13
            throw new DomainException("Cannot comment on a closed ticket.");

        var comment = new TicketComment(Id, author.Id, body, clock.GetUtcNow().UtcDateTime);
        _comments.Add(comment);
        return comment;
    }
}
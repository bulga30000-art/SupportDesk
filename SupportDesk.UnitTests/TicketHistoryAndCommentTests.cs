// SupportDesk.UnitTests/TicketHistoryAndCommentTests.cs
using Microsoft.Extensions.Time.Testing;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.ValueObjects;

namespace SupportDesk.UnitTests;

public class TicketHistoryAndCommentTests
{
    private static readonly TimeSpan Sla = TimeSpan.FromHours(24);
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero));

    private readonly Actor _customer = new(Guid.NewGuid(), UserRole.Customer);
    private readonly Actor _agent = new(Guid.NewGuid(), UserRole.SupportAgent);
    private readonly Actor _admin = new(Guid.NewGuid(), UserRole.Admin);

    private Ticket NewTicket() =>
        new("Printer broken", "No paper feed", Guid.NewGuid(), _customer.Id, TicketPriority.High, Sla, _clock);

    private Ticket InProgress()
    {
        var t = NewTicket();
        t.Assign(_admin, _agent, _clock);
        t.Start(_agent, _clock);
        return t;
    }

    // ---- History ----
    [Fact]
    public void New_ticket_has_a_created_history_row()
    {
        var t = NewTicket();

        var row = Assert.Single(t.History);
        Assert.Equal(TicketHistoryAction.Created, row.Action);
        Assert.Equal(_customer.Id, row.ChangedById);
        Assert.Equal(TicketStatus.Open, row.ToStatus);
        Assert.Null(row.FromStatus);
    }

    [Fact]
    public void Assign_records_who_was_assigned()
    {
        var t = NewTicket();
        t.Assign(_admin, _agent, _clock);

        var row = t.History.Last();
        Assert.Equal(TicketHistoryAction.Assigned, row.Action);
        Assert.Equal(_admin.Id, row.ChangedById);
        Assert.Equal(_agent.Id, row.AssigneeId);
    }

    [Fact]
    public void Resolve_records_the_status_change()
    {
        var t = InProgress();
        t.Resolve(_agent, _clock);

        var row = t.History.Last();
        Assert.Equal(TicketHistoryAction.Resolved, row.Action);
        Assert.Equal(TicketStatus.InProgress, row.FromStatus);
        Assert.Equal(TicketStatus.Resolved, row.ToStatus);
    }

    [Fact]
    public void Reopen_records_the_previous_deadline()
    {
        var t = InProgress();
        t.Resolve(_agent, _clock);
        var oldDeadline = t.ResolutionDeadline;
        _clock.Advance(TimeSpan.FromHours(30));

        t.Reopen(_customer, Sla, _clock);

        var row = t.History.Last();
        Assert.Equal(TicketHistoryAction.Reopened, row.Action);
        Assert.Equal(oldDeadline, row.PreviousDeadline);
        Assert.NotEqual(oldDeadline, t.ResolutionDeadline);
    }

    [Fact]
    public void Failed_operation_writes_no_history()
    {
        var t = NewTicket();
        t.Assign(_admin, _agent, _clock);
        var before = t.History.Count;

        Assert.Throws<DomainException>(() => t.Resolve(_agent, _clock));          // Open -> Resolved ممنوع
        Assert.Throws<ForbiddenActionException>(() => t.Start(_admin, _clock));   // مش الـ agent

        Assert.Equal(before, t.History.Count);
    }

    [Fact]
    public void History_cannot_be_modified_from_outside()
    {
        var t = NewTicket();

        Assert.IsNotType<List<TicketHistory>>(t.History);
        Assert.Throws<NotSupportedException>(() =>
            ((ICollection<TicketHistory>)t.History).Add(null!));
    }

    // ---- Comments ----
    [Fact]
    public void Can_add_a_comment()
    {
        var t = NewTicket();
        var comment = t.AddComment(_customer, "  Still broken  ", _clock);

        Assert.Single(t.Comments);
        Assert.Equal("Still broken", comment.Body);
        Assert.Equal(_customer.Id, comment.AuthorId);
        Assert.Equal(t.Id, comment.TicketId);
    }

    [Fact]
    public void Blank_comment_throws()
    {
        var t = NewTicket();
        Assert.Throws<DomainException>(() => t.AddComment(_customer, "   ", _clock));
        Assert.Empty(t.Comments);
    }

    [Fact]
    public void Cannot_comment_on_a_closed_ticket()   // BR-13
    {
        var t = InProgress();
        t.Resolve(_agent, _clock);
        t.Close(_customer, _clock);

        Assert.Throws<DomainException>(() => t.AddComment(_customer, "Thanks", _clock));
    }

    [Fact]
    public void Comments_cannot_be_modified_from_outside()
    {
        var t = NewTicket();
        Assert.Throws<NotSupportedException>(() =>
            ((ICollection<TicketComment>)t.Comments).Add(null!));
    }
}
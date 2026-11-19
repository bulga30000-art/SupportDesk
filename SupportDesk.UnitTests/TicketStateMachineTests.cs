using Microsoft.Extensions.Time.Testing;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.ValueObjects;

namespace SupportDesk.UnitTests;

public class TicketStateMachineTests
{
    private static readonly TimeSpan Sla = TimeSpan.FromHours(24);
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero));

    private readonly Actor _customer = new(Guid.NewGuid(), UserRole.Customer);
    private readonly Actor _otherCustomer = new(Guid.NewGuid(), UserRole.Customer);
    private readonly Actor _agent = new(Guid.NewGuid(), UserRole.SupportAgent);
    private readonly Actor _otherAgent = new(Guid.NewGuid(), UserRole.SupportAgent);
    private readonly Actor _admin = new(Guid.NewGuid(), UserRole.Admin);

    private Ticket NewTicket() =>
        new("Printer broken", "No paper feed", Guid.NewGuid(), _customer.Id, TicketPriority.High, Sla, _clock);

    private Ticket Assigned() { var t = NewTicket(); t.Assign(_admin, _agent, _clock); return t; }
    private Ticket InProgress() { var t = Assigned(); t.Start(_agent, _clock); return t; }
    private Ticket Resolved() { var t = InProgress(); t.Resolve(_agent, _clock); return t; }
    private Ticket Closed() { var t = Resolved(); t.Close(_customer, _clock); return t; }

    // ---- Assign / Claim ----
    [Fact]
    public void Admin_can_assign_to_agent_and_status_stays_open()
    {
        var t = Assigned();
        Assert.Equal(_agent.Id, t.AgentId);
        Assert.Equal(TicketStatus.Open, t.Status);
    }

    [Fact]
    public void Assign_to_non_agent_throws()
    {
        var t = NewTicket();
        Assert.Throws<DomainException>(() => t.Assign(_admin, _customer, _clock));
    }

    [Fact]
    public void Non_admin_cannot_assign()
    {
        var t = NewTicket();
        Assert.Throws<ForbiddenActionException>(() => t.Assign(_agent, _otherAgent, _clock));
    }

    [Fact]
    public void Cannot_assign_a_ticket_that_is_not_open()
    {
        var t = InProgress();
        Assert.Throws<DomainException>(() => t.Assign(_admin, _otherAgent, _clock));
    }

    [Fact]
    public void Agent_can_claim_unassigned_ticket()
    {
        var t = NewTicket();
        t.Claim(_agent, _clock);
        Assert.Equal(_agent.Id, t.AgentId);
    }

    [Fact]
    public void Claim_fails_when_already_assigned()
    {
        var t = Assigned();
        Assert.Throws<DomainException>(() => t.Claim(_otherAgent, _clock));
    }

    [Fact]
    public void Customer_cannot_claim()
    {
        var t = NewTicket();
        Assert.Throws<ForbiddenActionException>(() => t.Claim(_customer, _clock));
    }

    // ---- Start / Resolve ----
    [Fact]
    public void Assigned_agent_can_start()
    {
        Assert.Equal(TicketStatus.InProgress, InProgress().Status);
    }

    [Fact]
    public void Other_agent_cannot_start()
    {
        var t = Assigned();
        Assert.Throws<ForbiddenActionException>(() => t.Start(_otherAgent, _clock));
    }

    [Fact]
    public void Resolve_sets_ResolvedAt()
    {
        var t = InProgress();
        _clock.Advance(TimeSpan.FromHours(2));
        t.Resolve(_agent, _clock);

        Assert.Equal(TicketStatus.Resolved, t.Status);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, t.ResolvedAt);
    }

    [Fact]
    public void Cannot_resolve_an_open_ticket()      // Open -> Resolved غير مسموح
    {
        var t = Assigned();
        Assert.Throws<DomainException>(() => t.Resolve(_agent, _clock));
    }

    // ---- Close ----
    [Fact]
    public void Owner_can_close_resolved_ticket()
    {
        var t = Resolved();
        t.Close(_customer, _clock);
        Assert.Equal(TicketStatus.Closed, t.Status);
        Assert.NotNull(t.ClosedAt);
    }

    [Fact]
    public void Admin_can_close_resolved_ticket()
    {
        var t = Resolved();
        t.Close(_admin, _clock);
        Assert.Equal(TicketStatus.Closed, t.Status);
    }

    [Fact]
    public void Cannot_close_a_ticket_that_is_not_resolved()   // BR-10
    {
        Assert.Throws<DomainException>(() => InProgress().Close(_customer, _clock));
        Assert.Throws<DomainException>(() => NewTicket().Close(_admin, _clock));
    }

    [Fact]
    public void Other_customer_and_agent_cannot_close()
    {
        var t = Resolved();
        Assert.Throws<ForbiddenActionException>(() => t.Close(_otherCustomer, _clock));
        Assert.Throws<ForbiddenActionException>(() => t.Close(_agent, _clock));
    }

    // ---- Reopen ----
    [Fact]
    public void Owner_reopens_resolved_ticket_unassigns_and_resets_deadline()
    {
        var t = Resolved();
        _clock.Advance(TimeSpan.FromHours(30));          // عدّى الـ deadline القديم

        t.Reopen(_customer, Sla, _clock);

        Assert.Equal(TicketStatus.Open, t.Status);
        Assert.Null(t.AgentId);
        Assert.Null(t.ResolvedAt);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime + Sla, t.ResolutionDeadline);
        Assert.False(t.IsOverdue(_clock));
    }

    [Fact]
    public void Customer_cannot_reopen_closed_ticket()   // BR-12
    {
        var t = Closed();
        Assert.Throws<ForbiddenActionException>(() => t.Reopen(_customer, Sla, _clock));
    }

    [Fact]
    public void Admin_can_reopen_closed_ticket()
    {
        var t = Closed();
        t.Reopen(_admin, Sla, _clock);
        Assert.Equal(TicketStatus.Open, t.Status);
        Assert.Null(t.ClosedAt);
    }

    [Fact]
    public void Cannot_reopen_an_open_ticket()
    {
        Assert.Throws<DomainException>(() => NewTicket().Reopen(_admin, Sla, _clock));
    }

    // ---- BR-11: انتقالات ممنوعة ----
    [Fact]
    public void Failed_transition_leaves_state_unchanged()
    {
        var t = Assigned();
        Assert.Throws<DomainException>(() => t.Resolve(_agent, _clock));

        Assert.Equal(TicketStatus.Open, t.Status);
        Assert.Null(t.ResolvedAt);
    }
}
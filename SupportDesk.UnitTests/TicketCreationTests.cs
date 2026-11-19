using Microsoft.Extensions.Time.Testing;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;

namespace SupportDesk.UnitTests;

public class TicketCreationTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _clock = new(Start);
    private static readonly TimeSpan Sla = TimeSpan.FromHours(24);

    private Ticket Create(string title = "Printer broken", string description = "No paper feed") =>
        new(title, description, Guid.NewGuid(), Guid.NewGuid(), TicketPriority.High, Sla, _clock);

    [Fact]
    public void New_ticket_is_open_unassigned_and_has_deadline()
    {
        var ticket = Create();

        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Null(ticket.AgentId);
        Assert.Equal(Start.UtcDateTime, ticket.CreatedAt);
        Assert.Equal(Start.UtcDateTime + Sla, ticket.ResolutionDeadline);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_title_or_description_throws(string blank)
    {
        Assert.Throws<DomainException>(() => Create(title: blank));
        Assert.Throws<DomainException>(() => Create(description: blank));
    }

    [Fact]
    public void Empty_category_throws()
    {
        Assert.Throws<DomainException>(() =>
            new Ticket("t", "d", Guid.Empty, Guid.NewGuid(), TicketPriority.Low, Sla, _clock));
    }

    [Fact]
    public void Non_positive_resolution_time_throws()
    {
        Assert.Throws<DomainException>(() =>
            new Ticket("t", "d", Guid.NewGuid(), Guid.NewGuid(), TicketPriority.Low, TimeSpan.Zero, _clock));
    }

    [Fact]
    public void Is_not_overdue_exactly_at_deadline_but_is_after_it()
    {
        var ticket = Create();

        _clock.Advance(Sla);
        Assert.False(ticket.IsOverdue(_clock));

        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(ticket.IsOverdue(_clock));
    }
}
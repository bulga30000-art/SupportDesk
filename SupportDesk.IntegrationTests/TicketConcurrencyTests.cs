// SupportDesk.IntegrationTests/TicketConcurrencyTests.cs
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.ValueObjects;
using SupportDesk.Infrastructure.Persistence;   // عدّل حسب namespace الـ AppDbContext عندك

namespace SupportDesk.IntegrationTests;

public class TicketConcurrencyTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("SUPPORTDESK_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set SUPPORTDESK_TEST_CONNECTION to a SQL Server connection string (use a separate database, e.g. SupportDesk_Tests).");

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    private static async Task<Guid> SeedTicketAsync()
    {
        await using var db = NewContext();
        await db.Database.MigrateAsync();      // بيعمل الداتابيز لو مش موجودة، وبيطبق الـ migrations

        var department = new Department($"Dept {Guid.NewGuid()}");
        var category = new Category("Hardware", department.Id);
        var ticket = new Ticket("Printer broken", "No paper feed", category.Id,
            Guid.NewGuid(), TicketPriority.High, TimeSpan.FromHours(24), Clock);

        db.Departments.Add(department);
        db.Categories.Add(category);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }

    [Fact]
    public async Task Two_agents_claiming_the_same_ticket_only_one_succeeds()
    {
        var ticketId = await SeedTicketAsync();
        var agentA = new Actor(Guid.NewGuid(), UserRole.SupportAgent);
        var agentB = new Actor(Guid.NewGuid(), UserRole.SupportAgent);

        await using var dbA = NewContext();
        await using var dbB = NewContext();

        // الاتنين حمّلوا نفس النسخة (غير معيّنة، نفس الـ RowVersion)
        var ticketA = await dbA.Tickets.SingleAsync(t => t.Id == ticketId);
        var ticketB = await dbB.Tickets.SingleAsync(t => t.Id == ticketId);

        ticketA.Claim(agentA, Clock);
        ticketB.Claim(agentB, Clock);   // الـ Domain ماشفش التعارض، لأن نسخته قديمة

        await dbA.SaveChangesAsync();   // الأول ينجح
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());

        // التحقق من الحالة النهائية: واحد بس كسب، ومفيش صف History نص محفوظ
        await using var verify = NewContext();
        var saved = await verify.Tickets.Include(t => t.History)
            .SingleAsync(t => t.Id == ticketId);

        Assert.Equal(agentA.Id, saved.AgentId);
        Assert.Equal(2, saved.History.Count);   // Created + Claimed واحد بس
        Assert.Single(saved.History, h => h.Action == TicketHistoryAction.Claimed);
    }

    [Fact]
    public async Task After_a_conflict_a_fresh_claim_is_rejected_by_the_domain()
    {
        var ticketId = await SeedTicketAsync();
        var agentA = new Actor(Guid.NewGuid(), UserRole.SupportAgent);
        var agentB = new Actor(Guid.NewGuid(), UserRole.SupportAgent);

        await using (var db = NewContext())
        {
            var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
            ticket.Claim(agentA, Clock);
            await db.SaveChangesAsync();
        }

        // الـ agent التاني يحمّل من جديد: النسخة الحالية، فالـ Domain نفسه يرفض بسبب واضح
        await using var dbB = NewContext();
        var fresh = await dbB.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Throws<DomainException>(() => fresh.Claim(agentB, Clock));
    }
}
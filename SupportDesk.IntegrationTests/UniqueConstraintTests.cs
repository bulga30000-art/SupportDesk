// UniqueConstraintTests.cs
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Entities;
using SupportDesk.Infrastructure.Persistence;   // عدّل حسب namespace الـ AppDbContext

namespace SupportDesk.IntegrationTests;

public class UniqueConstraintTests
{
    private static string ConnectionString =>
       Environment.GetEnvironmentVariable("SUPPORTDESK_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set SUPPORTDESK_TEST_CONNECTION to a SQL Server connection string (use a separate database, e.g. SupportDesk_Tests).");

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    [Fact]
    public async Task Saving_a_duplicate_department_name_throws_ConflictException()
    {
        var name = $"Dept {Guid.NewGuid()}";

        await using (var db = NewContext())
        {
            await db.Database.MigrateAsync();
            db.Departments.Add(new Department(name));
            await db.SaveChangesAsync();
        }

        await using var db2 = NewContext();
        db2.Departments.Add(new Department(name));   // بيتخطى الفحص المسبق، زي السباق بالظبط

        await Assert.ThrowsAsync<ConflictException>(() => db2.SaveChangesAsync());
    }

    [Fact]
    public async Task Saving_a_duplicate_category_name_in_the_same_department_throws_ConflictException()
    {
        Guid departmentId;

        await using (var db = NewContext())
        {
            await db.Database.MigrateAsync();
            var department = new Department($"Dept {Guid.NewGuid()}");
            db.Departments.Add(department);
            db.Categories.Add(new Category("Hardware", department.Id));
            await db.SaveChangesAsync();
            departmentId = department.Id;
        }

        await using var db2 = NewContext();
        db2.Categories.Add(new Category("Hardware", departmentId));

        await Assert.ThrowsAsync<ConflictException>(() => db2.SaveChangesAsync());
    }
}
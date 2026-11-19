using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Entities;
using Microsoft.Data.SqlClient;
using SupportDesk.Application.Exceptions;

namespace SupportDesk.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    public override async Task<int> SaveChangesAsync(
    bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
{
    try
    {
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        throw new ConflictException("A record with the same unique value already exists.", ex);
    }
}

// 2601 = unique index، 2627 = unique constraint
private static bool IsUniqueViolation(DbUpdateException ex) =>
    ex.InnerException is SqlException { Number: 2601 or 2627 };
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
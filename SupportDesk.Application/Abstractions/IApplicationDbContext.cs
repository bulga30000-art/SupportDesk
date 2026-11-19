using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Category> Categories { get; }
    DbSet<Ticket> Tickets { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
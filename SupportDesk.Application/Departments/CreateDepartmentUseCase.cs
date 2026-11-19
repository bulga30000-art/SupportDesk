using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Entities;

namespace SupportDesk.Application.Departments;

public class CreateDepartmentUseCase
{
    private readonly IApplicationDbContext _db;

    public CreateDepartmentUseCase(IApplicationDbContext db) => _db = db;

    public async Task<DepartmentDto> ExecuteAsync(string name, CancellationToken ct)
    {
        var department = new Department(name);

        var exists = await _db.Departments.AnyAsync(d => d.Name == department.Name, ct);
        if (exists)
            throw new ConflictException($"Department '{department.Name}' already exists.");

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(ct);

        return new DepartmentDto(department.Id, department.Name);
    }
}
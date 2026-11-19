using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Entities;

public class CreateCategoryUseCase
{
    private readonly IApplicationDbContext _db;

    public CreateCategoryUseCase(IApplicationDbContext db) => _db = db;

    public async Task<CategoryDto> ExecuteAsync(string name, Guid departmentId, CancellationToken ct)
    {
        var category = new Category(name, departmentId);   // الـ Domain بيتحقق

        if (!await _db.Departments.AnyAsync(d => d.Id == departmentId, ct))
            throw new NotFoundException("Department not found.");

        if (await _db.Categories.AnyAsync(
                c => c.DepartmentId == departmentId && c.Name == category.Name, ct))
            throw new ConflictException("A category with this name already exists in the department.");

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        return new CategoryDto(category.Id, category.Name, category.DepartmentId);
    }
}
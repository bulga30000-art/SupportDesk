using SupportDesk.Domain.Exceptions;

namespace SupportDesk.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid DepartmentId { get; private set; }

    private Category() { }   // For EF Core only

    public Category(string name, Guid departmentId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");
        if (departmentId == Guid.Empty)
            throw new DomainException("Department is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        DepartmentId = departmentId;
    }
}
using SupportDesk.Domain.Exceptions;

namespace SupportDesk.Domain.Entities;

public class Department
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;

    private Department() { } //to ef coer

    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Department name is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
    }
}
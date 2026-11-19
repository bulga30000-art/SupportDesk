using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Exceptions;

public class DepartmentTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string name)
        => Assert.Throws<DomainException>(() => new Department(name));

    [Fact]
    public void Constructor_TrimsName()
        => Assert.Equal("Billing", new Department("  Billing ").Name);
}
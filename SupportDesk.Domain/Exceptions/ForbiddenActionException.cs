namespace SupportDesk.Domain.Exceptions;

public class ForbiddenActionException : DomainException
{
    public ForbiddenActionException(string message) : base(message) { }
}
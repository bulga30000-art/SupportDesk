// TempActor.cs
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Exceptions;
using SupportDesk.Domain.ValueObjects;

// مؤقت جدًا: بيتمسح في المرحلة 5 وبيتبدل بـ JWT claims. أي حد يقدر يدّعي أي دور.
internal static class TempActor
{
    public static Actor From(HttpRequest request)
    {
        if (!Guid.TryParse(request.Headers["X-Actor-Id"], out var id)
            || !Enum.TryParse<UserRole>(request.Headers["X-Actor-Role"], true, out var role)
            || !Enum.IsDefined(role))
            throw new DomainException("X-Actor-Id and X-Actor-Role headers are required (temporary).");

        return new Actor(id, role);
    }
}
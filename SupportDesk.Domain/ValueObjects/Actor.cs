using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.ValueObjects;

/// الشخص اللي بينفّذ العملية: هويته ودوره
public readonly record struct Actor(Guid Id, UserRole Role);
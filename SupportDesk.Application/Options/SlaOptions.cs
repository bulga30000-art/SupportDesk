using System.ComponentModel.DataAnnotations;
using SupportDesk.Domain.Enums;

namespace SupportDesk.Application.Options;

public class SlaOptions
{
    public const string SectionName = "Sla";

    [Range(1, 1000)] public int LowHours { get; init; }
    [Range(1, 1000)] public int MediumHours { get; init; }
    [Range(1, 1000)] public int HighHours { get; init; }
    [Range(1, 1000)] public int UrgentHours { get; init; }

    public TimeSpan For(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => TimeSpan.FromHours(LowHours),
        TicketPriority.Medium => TimeSpan.FromHours(MediumHours),
        TicketPriority.High => TimeSpan.FromHours(HighHours),
        TicketPriority.Urgent => TimeSpan.FromHours(UrgentHours),
        _ => throw new ArgumentOutOfRangeException(nameof(priority))
    };
}
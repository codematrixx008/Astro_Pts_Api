namespace Astro.Domain.Marketplace;

public sealed record AstrologerAvailability(
    long AvailabilityId,
    long AstrologerId,
    int DayOfWeek,
    System.TimeSpan StartTime,
    System.TimeSpan EndTime,
    bool IsActive,
    System.DateTime CreatedUtc
);

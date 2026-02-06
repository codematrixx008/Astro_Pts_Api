namespace Astro.Application.Marketplace;

public sealed record ApplyAstrologerRequest(
    string DisplayName,
    string? Bio,
    int ExperienceYears,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> Specializations,
    decimal PricePerMinute
);

public sealed record AvailabilityCreateRequest(
    int DayOfWeek,
    string StartTime,
    string EndTime
);

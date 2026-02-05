namespace Astro.Domain.Marketplace;

public sealed record AstrologerProfile(
    long AstrologerId,
    string DisplayName,
    string? Bio,
    int ExperienceYears,
    string LanguagesCsv,
    string SpecializationsCsv,
    decimal PricePerMinute,
    string Status, // applied|verified|active|suspended
    DateTime CreatedUtc,
    DateTime? VerifiedUtc
);

public sealed record AstrologerAvailability(
    long AvailabilityId,
    long AstrologerId,
    int DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsActive,
    DateTime CreatedUtc
);

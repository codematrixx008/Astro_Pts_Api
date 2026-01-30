namespace Astro.Domain.Marketplace;

public sealed record AstrologerProfile(
    long AstrologerId,
    string DisplayName,
    string? Bio,
    int? ExperienceYears,
    string? LanguagesCsv,
    string? SpecializationsCsv,
    decimal PricePerMinute,
    string Status,             // applied, verified, active, suspended
    System.DateTime CreatedUtc,
    System.DateTime? VerifiedUtc
);

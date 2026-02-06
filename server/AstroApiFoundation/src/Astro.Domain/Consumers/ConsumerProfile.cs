namespace Astro.Domain.Consumers;

public sealed record ConsumerProfile(
    long UserId,
    string FullName,
    string? Gender,
    string? Phone,
    string? MaritalStatus,
    string? Occupation,
    string? City,
    string? PreferredLanguage,
    DateTime UpdatedUtc
);

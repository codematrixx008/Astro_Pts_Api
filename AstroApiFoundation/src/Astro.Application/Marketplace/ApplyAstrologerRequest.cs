namespace Astro.Application.Marketplace;

public sealed class ApplyAstrologerRequest
{
    public string DisplayName { get; init; } = default!;
    public string? Bio { get; init; }
    public int? ExperienceYears { get; init; }
    public string[] Languages { get; init; } = [];
    public string[] Specializations { get; init; } = [];
    public decimal PricePerMinute { get; init; }
}

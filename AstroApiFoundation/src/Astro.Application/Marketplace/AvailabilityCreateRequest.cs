namespace Astro.Application.Marketplace;

public sealed class AvailabilityCreateRequest
{
    public int DayOfWeek { get; init; }      // 0..6
    public string StartTime { get; init; } = default!; // "09:00"
    public string EndTime { get; init; } = default!;   // "12:00"
}

namespace Astro.Domain.Models;

public sealed class OtherImportantData
{
    public int Id { get; set; }
    public string? MainHeading { get; set; }
    public string? Image { get; set; }
    public string Heading { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

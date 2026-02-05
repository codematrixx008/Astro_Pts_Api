namespace Astro.Domain.Models;

public sealed class PersonalDetail
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

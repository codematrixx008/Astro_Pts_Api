namespace Astro.Application.Chat;

public sealed class ChatRequestCreateRequest
{
    public long AstrologerId { get; init; }
    public string? Topic { get; init; }
}

namespace Astro.Application.Chat;

public sealed class SendMessageRequest
{
    public string Message { get; init; } = default!;
}

namespace Astro.Domain.Consumers;

public interface IConsumerProfileRepository
{
    Task<ConsumerProfile?> GetByUserIdAsync(long userId, CancellationToken ct);
    Task UpsertAsync(ConsumerProfile profile, CancellationToken ct);
}

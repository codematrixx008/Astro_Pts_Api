using Astro.Domain.Models;

namespace Astro.Domain.Interface;

public interface IMaleficRepository
{
    Task<IEnumerable<Malefic>> GetMaleficsAsync(
        CancellationToken ct = default);
}

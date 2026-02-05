using Astro.Domain.Models;

namespace Astro.Domain.Interface;

public interface IAvkahadaChakraRepository
{
    Task<IEnumerable<AvkahadaChakra>> GetAvkahadaChakraAsync(
        CancellationToken ct = default);
}


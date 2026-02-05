using Astro.Domain.Models;

namespace Astro.Domain.Interface;

public interface IOtherImportantDataRepository
{
    Task<IEnumerable<OtherImportantData>> GetOtherImportantDataAsync(
        CancellationToken ct = default);
}



using Astro.Domain.Models;

namespace Astro.Domain.Interface;

public interface IPersonalDetailRepository
{
    Task<IEnumerable<PersonalDetail>> GetPersonalDetailsAsync(
        CancellationToken ct = default);
}



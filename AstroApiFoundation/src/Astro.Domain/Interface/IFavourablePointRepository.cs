using Astro.Domain.Models;

namespace Astro.Domain.Interface;

public interface IFavourablePointRepository
{
    Task<IEnumerable<FavourablePoint>> GetFavourablePointsAsync(
        CancellationToken ct = default);
}

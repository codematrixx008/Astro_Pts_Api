namespace Astro.Application.Ephemeris;

public interface IEphemerisService
{
    Task<PlanetPositionResponse> GetPlanetPositionsAsync(PlanetPositionRequest req, CancellationToken ct);
}

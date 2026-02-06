using System.Security.Cryptography;

namespace Astro.Application.Ephemeris;

/// <summary>
/// Placeholder engine (deterministic) so you can ship the API platform first.
/// Replace with Swiss Ephemeris / professional engine later.
/// </summary>
public sealed class PlaceholderEphemerisService : IEphemerisService
{
    private static readonly string[] Planets = new[]
    {
        "Sun","Moon","Mars","Mercury","Jupiter","Venus","Saturn","Rahu","Ketu"
    };

    public Task<PlanetPositionResponse> GetPlanetPositionsAsync(PlanetPositionRequest req, CancellationToken ct)
    {
        // deterministic pseudo positions based on timestamp
        var ticksBytes = BitConverter.GetBytes(req.DateTimeUtc.ToUniversalTime().Ticks);
        var seed = SHA256.HashData(ticksBytes);

        var items = new List<PlanetPositionItem>(Planets.Length);
        for (var i = 0; i < Planets.Length; i++)
        {
            var a = seed[(i * 3) % seed.Length];
            var b = seed[(i * 3 + 1) % seed.Length];
            var c = seed[(i * 3 + 2) % seed.Length];

            var lon = (a / 255.0) * 360.0;
            var lat = ((b / 255.0) * 10.0) - 5.0; // -5..+5
            var spd = ((c / 255.0) * 2.0) - 1.0; // -1..+1 deg/day

            items.Add(new PlanetPositionItem(Planets[i], Math.Round(lon, 6), Math.Round(lat, 6), Math.Round(spd, 6)));
        }

        var resp = new PlanetPositionResponse(
            InputUtc: req.DateTimeUtc.ToUniversalTime(),
            Engine: "placeholder-v1",
            Planets: items
        );

        return Task.FromResult(resp);
    }
}

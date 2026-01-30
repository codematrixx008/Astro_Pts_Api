namespace Astro.Application.Ephemeris;

public sealed record PlanetPositionRequest(
    DateTime DateTimeUtc
);

public sealed record PlanetPositionItem(
    string Planet,
    double LongitudeDeg,
    double LatitudeDeg,
    double SpeedDegPerDay
);

public sealed record PlanetPositionResponse(
    DateTime InputUtc,
    string Engine,
    IReadOnlyList<PlanetPositionItem> Planets
);

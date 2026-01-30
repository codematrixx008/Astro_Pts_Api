namespace Astro.Application.Common;

public static class Validation
{
    public static void EnsureUtcAndRange(DateTime dtUtc)
    {
        if (dtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("datetimeUtc must be UTC.");

        var min = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var max = new DateTime(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        if (dtUtc < min || dtUtc > max)
            throw new ArgumentOutOfRangeException(nameof(dtUtc), "datetimeUtc out of range.");
    }

    public static void EnsureLatLon(double lat, double lon)
    {
        if (lat < -90 || lat > 90)
            throw new ArgumentOutOfRangeException(nameof(lat), "lat must be -90..90");
        if (lon < -180 || lon > 180)
            throw new ArgumentOutOfRangeException(nameof(lon), "lon must be -180..180");
    }

    public static void EnsureDateRange(DateOnly date)
    {
        var min = new DateOnly(1900, 1, 1);
        var max = new DateOnly(2100, 12, 31);

        if (date < min || date > max)
            throw new ArgumentOutOfRangeException(nameof(date), "date out of range.");
    }

}

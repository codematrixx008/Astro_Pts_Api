using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Application.Common
{
    public static class Validation
    {
        public static bool IsUtc(DateTime dt) => dt.Kind == DateTimeKind.Utc;

        public static void EnsureUtcAndRange(DateTime dtUtc)
        {
            if (dtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("datetimeUtc must be in UTC (DateTimeKind.Utc).");

            var min = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var max = new DateTime(2100, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            if (dtUtc < min || dtUtc > max)
                throw new ArgumentOutOfRangeException(nameof(dtUtc), "datetimeUtc must be between 1900 and 2100.");
        }

        public static void EnsureLatLon(double lat, double lon)
        {
            if (lat < -90 || lat > 90) throw new ArgumentOutOfRangeException(nameof(lat), "lat must be -90..90");
            if (lon < -180 || lon > 180) throw new ArgumentOutOfRangeException(nameof(lon), "lon must be -180..180");
        }
    }

}

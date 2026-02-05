using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Domain.Models
{
    public sealed class CalChalitResponse
    {
        public string? MasterHeading { get; set; }
        public IEnumerable<CalChalitColumn> Columns { get; set; } = [];
        public IEnumerable<CalChalitRow> Rows { get; set; } = [];
    }

}

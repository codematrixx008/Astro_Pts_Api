using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Domain.Models
{
    public class PrastharashtakvargaResponseDto
    {
        public List<PrastharashtakvargaDto> Prastharashtakvarga { get; set; }
    }

    public class PrastharashtakvargaDto
    {
        public string MasterHeading { get; set; }
        public List<ColumnDto> Columns { get; set; }
        public List<RowDto> Rows { get; set; }
    }

    public class ColumnDto
    {  
        public string Key { get; set; }
        public string Label { get; set; }

    }


    public class RowDto
    {
        public string MasterHeading { get; set; }
        public string Planet { get; set; }
        public int? Ar { get; set; }
        public int? Ta { get; set; }
        public int? Ge { get; set; }
        public int? Ca { get; set; }
        public int? Le { get; set; }
        public int? Vi { get; set; }
        public int? Li { get; set; }
        public int? Sc { get; set; }
        public int? Sa { get; set; }
        public int? Cp { get; set; }
        public int? Aq { get; set; }
        public int? Pi { get; set; }
        public int? Total { get; set; }
    };
}



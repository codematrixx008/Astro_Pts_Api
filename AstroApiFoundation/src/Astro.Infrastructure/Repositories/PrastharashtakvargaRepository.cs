using Astro.Domain.Interface;
using Astro.Domain.Models;
using Astro.Infrastructure.Data;
using Azure;
using Dapper;
using System.Data;

public class PrastharashtakvargaRepository : IPrastharashtakvargaRepository
{
    private readonly IDbConnectionFactory _db;

    public PrastharashtakvargaRepository(IDbConnectionFactory db)
    {
        _db = db;
    }
    private static List<ColumnDto> GetDefaultColumns()
    {
        var listCols = new List<ColumnDto>
    {
        new ColumnDto { Key = "planet", Label = "" },
        new ColumnDto { Key = "ar", Label = "Ar" },
        new ColumnDto { Key = "ta", Label = "Ta" },
        new ColumnDto { Key = "ge", Label = "Ge" },
        new ColumnDto { Key = "ca", Label = "Ca" },
        new ColumnDto { Key = "le", Label = "Le" },
        new ColumnDto { Key = "vi", Label = "Vi" },
        new ColumnDto { Key = "li", Label = "Li" },
        new ColumnDto { Key = "sc", Label = "Sc" },
        new ColumnDto { Key = "sa", Label = "Sa" },
        new ColumnDto { Key = "cp", Label = "Cp" },
        new ColumnDto { Key = "aq", Label = "Aq" },
        new ColumnDto { Key = "pi", Label = "Pi" },
        new ColumnDto { Key = "total", Label = "Total" }
    };
        return listCols;
    }
    public async Task<PrastharashtakvargaResponseDto> GetDataAsync()
    {
       
        await using var connection = _db.Create();
            var dbResult = await connection.QueryAsync<RowDto>(
                "sp_GetPrastharashtakvarga",
                commandType: CommandType.StoredProcedure
            );

            var response = dbResult
                .GroupBy(x => x.MasterHeading)
                .Select(g => new PrastharashtakvargaDto
                {
                    MasterHeading = g.Key,
                    Columns = GetDefaultColumns(),
                    Rows = g.Select(x => new RowDto
                    {
                        Planet = x.Planet,
                        MasterHeading = x.MasterHeading,
                        Ar = x.Ar,
                        Ta = x.Ta,
                        Ge = x.Ge,
                        Ca = x.Ca,
                        Le = x.Le,
                        Vi = x.Vi,
                        Li = x.Li,
                        Sc = x.Sc,
                        Sa = x.Sa,
                        Cp = x.Cp,
                        Aq = x.Aq,
                        Pi = x.Pi,
                        Total = x.Total
                    }).ToList()
                })
                .ToList();
            PrastharashtakvargaResponseDto prd = new PrastharashtakvargaResponseDto { Prastharashtakvarga = response };

        return prd;
    }

   
}

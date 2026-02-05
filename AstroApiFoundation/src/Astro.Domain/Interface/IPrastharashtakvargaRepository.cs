using Astro.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astro.Domain.Interface
{
    public interface IPrastharashtakvargaRepository
    {
        Task<PrastharashtakvargaResponseDto> GetDataAsync();
    }

}

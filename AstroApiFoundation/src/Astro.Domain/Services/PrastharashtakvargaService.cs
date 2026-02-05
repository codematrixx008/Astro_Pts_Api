using Astro.Domain.Interface;
using Astro.Domain.Models;

public class PrastharashtakvargaService : IPrastharashtakvargaService
{
    private readonly IPrastharashtakvargaRepository _repo;

    public PrastharashtakvargaService(IPrastharashtakvargaRepository repo)
    {
        _repo = repo;
    }

    public async Task<PrastharashtakvargaResponseDto> GetAsync()
    {
        return await _repo.GetDataAsync();
    }
}

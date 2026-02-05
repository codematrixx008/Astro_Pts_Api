using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PrastharashtakvargaController : ControllerBase
{
    private readonly IPrastharashtakvargaService _service;

    public PrastharashtakvargaController(IPrastharashtakvargaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetAsync();
        return Ok(result);
    }
}

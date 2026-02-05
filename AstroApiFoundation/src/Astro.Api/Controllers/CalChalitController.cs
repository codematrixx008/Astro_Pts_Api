using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public sealed class CalChalitController : ControllerBase
{
    private readonly ICalChalitRepository _repository;

    public CalChalitController(ICalChalitRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _repository.GetCalChalitAsync(ct);
        return Ok(result);
    }
}



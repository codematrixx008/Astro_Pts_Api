using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OtherImportantDataController : ControllerBase
{
    private readonly IOtherImportantDataRepository _repository;

    public OtherImportantDataController(
        IOtherImportantDataRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _repository
            .GetOtherImportantDataAsync(cancellationToken);

        return Ok(result);
    }
}

using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MaleficsController : ControllerBase
{
    private readonly IMaleficRepository _repository;

    public MaleficsController(
        IMaleficRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _repository
            .GetMaleficsAsync(cancellationToken);

        return Ok(result);
    }
}

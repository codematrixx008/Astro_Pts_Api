using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AvkahadaChakraController : ControllerBase
{
    private readonly IAvkahadaChakraRepository _repository;

    public AvkahadaChakraController(
        IAvkahadaChakraRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _repository
            .GetAvkahadaChakraAsync(cancellationToken);

        return Ok(result);
    }
}



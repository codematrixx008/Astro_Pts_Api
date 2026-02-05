using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PersonalDetailsController : ControllerBase
{
    private readonly IPersonalDetailRepository _repository;

    public PersonalDetailsController(
        IPersonalDetailRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _repository
            .GetPersonalDetailsAsync(cancellationToken);

        return Ok(result);
    }
}

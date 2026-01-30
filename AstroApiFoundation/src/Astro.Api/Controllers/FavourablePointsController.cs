using Astro.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Astro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FavourablePointsController : ControllerBase
{
    private readonly IFavourablePointRepository _repository;

    public FavourablePointsController(
        IFavourablePointRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _repository
            .GetFavourablePointsAsync(cancellationToken);

        return Ok(result);
    }
}

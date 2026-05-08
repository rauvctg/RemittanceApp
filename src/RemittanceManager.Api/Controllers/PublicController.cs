using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemittanceManager.Application.Remittances;
using RemittanceManager.Application.Remittances.Dtos;

namespace RemittanceManager.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicController(IRemittanceService remittances) : ControllerBase
{
    private readonly IRemittanceService _remittances = remittances;

    [HttpGet("track/{code}")]
    [ProducesResponseType(typeof(TrackingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackingResult>> Track(string code, CancellationToken ct)
    {
        var result = await _remittances.GetByTrackingCodeAsync(code, ct);
        if (result is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return new EmptyResult();
        }

        return Ok(result);
    }
}

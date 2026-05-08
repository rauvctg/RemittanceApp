using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemittanceManager.Api.Auth;
using RemittanceManager.Application.Remittances;
using RemittanceManager.Application.Remittances.Dtos;

namespace RemittanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/remittances")]
public class RemittancesController(IRemittanceService service, ICurrentUserService currentUser) : ControllerBase
{
    private readonly IRemittanceService _service = service;
    private readonly ICurrentUserService _currentUser = currentUser;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RemittanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RemittanceDto>>> List(CancellationToken ct)
    {
        var items = await _service.ListAsync(_currentUser.GetUserId(), ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RemittanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemittanceDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(_currentUser.GetUserId(), id, ct);
        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RemittanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RemittanceDto>> Create([FromBody] CreateRemittanceRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(_currentUser.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RemittanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemittanceDto>> Update(Guid id, [FromBody] UpdateRemittanceRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(_currentUser.GetUserId(), id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(_currentUser.GetUserId(), id, ct);
        return NoContent();
    }
}

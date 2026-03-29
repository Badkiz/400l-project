using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HostelMS.DTOs;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnnouncementController : ControllerBase
{
    private readonly IAnnouncementService _svc;
    public AnnouncementController(IAnnouncementService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _svc.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { message = "Title and body are required." });

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result  = await _svc.CreateAsync(adminId, request);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? Ok(new { message = "Announcement deleted." }) : NotFound(new { message = "Not found." });
    }

    [HttpPatch("{id}/pin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TogglePin(int id)
    {
        var ok = await _svc.TogglePinAsync(id);
        return ok ? Ok(new { message = "Pin toggled." }) : NotFound(new { message = "Not found." });
    }
}

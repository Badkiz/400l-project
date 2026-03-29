using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HostelMS.DTOs;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _svc;
    public ProfileController(IProfileService svc) => _svc = svc;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _svc.GetProfileAsync(CurrentUserId);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var result = await _svc.UpdateProfileAsync(CurrentUserId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (request.NewPassword.Length < 8)
            return BadRequest(new { message = "New password must be at least 8 characters." });

        try
        {
            await _svc.ChangePasswordAsync(CurrentUserId, request);
            return Ok(new { message = "Password updated successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("students")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStudents() =>
        Ok(await _svc.GetAllStudentsAsync());

    [HttpGet("students/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStudent(int id)
    {
        var student = await _svc.GetStudentDetailAsync(id);
        if (student == null) return NotFound(new { message = "Student not found." });
        return Ok(student);
    }
}

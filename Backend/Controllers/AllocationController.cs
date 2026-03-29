using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AllocationController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyAllocation()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var allocation = await _allocationService.GetUserAllocationAsync(userId);
        if (allocation == null) return NotFound(new { message = "No active allocation found." });
        return Ok(allocation);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAllocations()
    {
        var allocations = await _allocationService.GetAllAllocationsAsync();
        return Ok(allocations);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deallocate(int id)
    {
        var result = await _allocationService.DeallocateAsync(id);
        if (!result) return NotFound(new { message = "Allocation not found." });
        return Ok(new { message = "Student deallocated successfully." });
    }
}

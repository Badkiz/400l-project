using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HostelMS.DTOs;
using HostelMS.Interfaces;

namespace HostelMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetRooms([FromQuery] bool activeOnly = true)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var rooms = await _roomService.GetAllRoomsAsync(role == "Admin" ? activeOnly : true);
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null) return NotFound(new { message = "Room not found." });
        return Ok(room);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var room = await _roomService.CreateRoomAsync(request);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        var room = await _roomService.UpdateRoomAsync(id, request);
        if (room == null) return NotFound(new { message = "Room not found." });
        return Ok(room);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var result = await _roomService.DeleteRoomAsync(id);
        if (!result) return NotFound(new { message = "Room not found." });
        return Ok(new { message = "Room deactivated." });
    }
}

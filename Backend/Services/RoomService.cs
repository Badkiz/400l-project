using Microsoft.EntityFrameworkCore;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Interfaces;
using HostelMS.Models;

namespace HostelMS.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    private static RoomDto ToDto(Room r) => new(
        r.Id, r.RoomNumber, r.HostelBlock, r.RoomType,
        r.Capacity, r.OccupiedSlots, r.AvailableSlots,
        r.Price, r.Description, r.IsActive
    );

    public async Task<List<RoomDto>> GetAllRoomsAsync(bool activeOnly = true)
    {
        var query = _db.Rooms.AsQueryable();
        if (activeOnly) query = query.Where(r => r.IsActive);
        return await query.OrderBy(r => r.HostelBlock).ThenBy(r => r.RoomNumber)
                          .Select(r => ToDto(r)).ToListAsync();
    }

    public async Task<RoomDto?> GetRoomByIdAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        return room == null ? null : ToDto(room);
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomRequest request)
    {
        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == request.RoomNumber && r.HostelBlock == request.HostelBlock))
            throw new InvalidOperationException("Room already exists in this block.");

        var room = new Room
        {
            RoomNumber = request.RoomNumber.Trim(),
            HostelBlock = request.HostelBlock.Trim(),
            RoomType = request.RoomType.Trim(),
            Capacity = request.Capacity,
            Price = request.Price,
            Description = request.Description?.Trim()
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return ToDto(room);
    }

    public async Task<RoomDto?> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return null;

        if (request.RoomNumber != null) room.RoomNumber = request.RoomNumber.Trim();
        if (request.HostelBlock != null) room.HostelBlock = request.HostelBlock.Trim();
        if (request.RoomType != null) room.RoomType = request.RoomType.Trim();
        if (request.Capacity.HasValue) room.Capacity = request.Capacity.Value;
        if (request.Price.HasValue) room.Price = request.Price.Value;
        if (request.Description != null) room.Description = request.Description.Trim();
        if (request.IsActive.HasValue) room.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return ToDto(room);
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return false;
        room.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

using Microsoft.EntityFrameworkCore;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Interfaces;
using HostelMS.Models;

namespace HostelMS.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _db;
    public EventService(AppDbContext db) => _db = db;

    private static EventDto ToDto(HostelEvent e) => new(
        e.Id, e.Title, e.Description, e.Category,
        e.EventDate, e.EventTime, e.CreatedBy.FullName, e.CreatedAt
    );

    public async Task<List<EventDto>> GetAllAsync() =>
        await _db.Events
            .Include(e => e.CreatedBy)
            .Where(e => e.IsActive)
            .OrderBy(e => e.EventDate)
            .Select(e => ToDto(e))
            .ToListAsync();

    public async Task<EventDto> CreateAsync(int adminId, CreateEventRequest request)
    {
        var ev = new HostelEvent
        {
            Title             = request.Title.Trim(),
            Description       = request.Description.Trim(),
            Category          = request.Category,
            EventDate         = request.EventDate,
            EventTime         = request.EventTime.Trim(),
            CreatedByUserId   = adminId
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.CreatedBy).LoadAsync();
        return ToDto(ev);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return false;
        ev.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}

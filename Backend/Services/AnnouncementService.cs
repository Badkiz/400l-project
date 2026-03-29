using Microsoft.EntityFrameworkCore;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Interfaces;
using HostelMS.Models;

namespace HostelMS.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly AppDbContext _db;
    public AnnouncementService(AppDbContext db) => _db = db;

    private static AnnouncementDto ToDto(Announcement a) => new(
        a.Id, a.Title, a.Body, a.Category, a.IsPinned,
        a.CreatedBy.FullName, a.CreatedAt
    );

    public async Task<List<AnnouncementDto>> GetAllAsync() =>
        await _db.Announcements
            .Include(a => a.CreatedBy)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a))
            .ToListAsync();

    public async Task<AnnouncementDto> CreateAsync(int adminId, CreateAnnouncementRequest request)
    {
        var ann = new Announcement
        {
            Title             = request.Title.Trim(),
            Body              = request.Body.Trim(),
            Category          = request.Category,
            IsPinned          = request.IsPinned,
            CreatedByUserId   = adminId
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();
        await _db.Entry(ann).Reference(a => a.CreatedBy).LoadAsync();
        return ToDto(ann);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ann = await _db.Announcements.FindAsync(id);
        if (ann == null) return false;
        ann.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePinAsync(int id)
    {
        var ann = await _db.Announcements.FindAsync(id);
        if (ann == null) return false;
        ann.IsPinned = !ann.IsPinned;
        await _db.SaveChangesAsync();
        return true;
    }
}

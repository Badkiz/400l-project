using Microsoft.EntityFrameworkCore;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Interfaces;

namespace HostelMS.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    public ProfileService(AppDbContext db) => _db = db;

    public async Task<ProfileDto?> GetProfileAsync(int userId)
    {
        var u = await _db.Users.FindAsync(userId);
        if (u == null) return null;
        return new ProfileDto(u.Id, u.FullName, u.Email, u.MatricNumber, u.PhoneNumber, u.Role, u.CreatedAt);
    }

    public async Task<ProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var u = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName))
            u.FullName = request.FullName.Trim();
        if (request.PhoneNumber != null)
            u.PhoneNumber = request.PhoneNumber.Trim();

        await _db.SaveChangesAsync();
        return new ProfileDto(u.Id, u.FullName, u.Email, u.MatricNumber, u.PhoneNumber, u.Role, u.CreatedAt);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var u = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, u.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        if (request.NewPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");

        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
    }

    public async Task<List<StudentDto>> GetAllStudentsAsync()
    {
        var students = await _db.Users
            .Where(u => u.Role == "Student")
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var allocatedUserIds = await _db.Allocations
            .Where(a => a.IsActive)
            .Select(a => a.UserId)
            .ToListAsync();

        return students.Select(u => new StudentDto(
            u.Id, u.FullName, u.Email, u.MatricNumber, u.PhoneNumber,
            u.CreatedAt, allocatedUserIds.Contains(u.Id)
        )).ToList();
    }
    public async Task<StudentDetailDto?> GetStudentDetailAsync(int studentId)
    {
        var u = await _db.Users.FindAsync(studentId);
        if (u == null || u.Role != "Student") return null;

        var allocation = await _db.Allocations
            .Include(a => a.Room)
            .Include(a => a.Payment)
            .Include(a => a.User)
            .Where(a => a.UserId == studentId && a.IsActive)
            .Select(a => new AllocationDto(
                a.Id, a.UserId, a.User.FullName, a.User.Email, a.User.MatricNumber,
                a.RoomId, a.Room.RoomNumber, a.Room.HostelBlock, a.Room.RoomType,
                a.PaymentId, a.Payment.Reference, a.IsActive, a.AllocatedAt))
            .FirstOrDefaultAsync();

        var payments = await _db.Payments
            .Include(p => p.Room)
            .Where(p => p.UserId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id, p.Reference, p.Status, p.Amount,
                p.RoomId, p.Room.RoomNumber, p.CreatedAt, p.VerifiedAt))
            .ToListAsync();

        return new StudentDetailDto(
            u.Id, u.FullName, u.Email, u.MatricNumber, u.PhoneNumber,
            u.CreatedAt, allocation != null, allocation, payments);
    }

}

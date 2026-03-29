using Microsoft.EntityFrameworkCore;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Interfaces;
using HostelMS.Models;

namespace HostelMS.Services;

public class AllocationService : IAllocationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AllocationService> _logger;

    // Static lock per room to prevent concurrent allocation of same room
    private static readonly Dictionary<int, SemaphoreSlim> _roomLocks = new();
    private static readonly object _lockDictLock = new();

    public AllocationService(AppDbContext db, ILogger<AllocationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private SemaphoreSlim GetRoomLock(int roomId)
    {
        lock (_lockDictLock)
        {
            if (!_roomLocks.ContainsKey(roomId))
                _roomLocks[roomId] = new SemaphoreSlim(1, 1);
            return _roomLocks[roomId];
        }
    }

    public async Task<AllocationDto?> AllocateRoomAsync(int userId, int paymentId)
    {
        var payment = await _db.Payments
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.Status == "Success")
            ?? throw new InvalidOperationException("Valid payment not found.");

        var roomLock = GetRoomLock(payment.RoomId);
        await roomLock.WaitAsync();
        try
        {
            // Re-fetch inside lock to get fresh state
            var room = await _db.Rooms.FindAsync(payment.RoomId)
                ?? throw new InvalidOperationException("Room not found.");

            // Check user doesn't already have allocation
            var existingAllocation = await _db.Allocations
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);
            if (existingAllocation != null)
            {
                _logger.LogWarning("User {UserId} already has active allocation.", userId);
                return await GetAllocationDtoAsync(existingAllocation.Id);
            }

            // Check payment not already allocated
            if (await _db.Allocations.AnyAsync(a => a.PaymentId == paymentId))
            {
                _logger.LogWarning("Payment {PaymentId} already allocated.", paymentId);
                return null;
            }

            // Verify room still has space
            if (room.AvailableSlots <= 0)
                throw new InvalidOperationException("Room is fully occupied. Your payment will be refunded.");

            var allocation = new Allocation
            {
                UserId = userId,
                RoomId = payment.RoomId,
                PaymentId = paymentId,
                IsActive = true
            };

            room.OccupiedSlots += 1;
            _db.Allocations.Add(allocation);

            await _db.SaveChangesAsync();
            _logger.LogInformation("Allocated Room {RoomId} to User {UserId}", payment.RoomId, userId);

            return await GetAllocationDtoAsync(allocation.Id);
        }
        finally
        {
            roomLock.Release();
        }
    }

    private async Task<AllocationDto?> GetAllocationDtoAsync(int allocationId)
    {
        return await _db.Allocations
            .Include(a => a.User)
            .Include(a => a.Room)
            .Include(a => a.Payment)
            .Where(a => a.Id == allocationId)
            .Select(a => new AllocationDto(
                a.Id, a.UserId, a.User.FullName, a.User.Email, a.User.MatricNumber,
                a.RoomId, a.Room.RoomNumber, a.Room.HostelBlock, a.Room.RoomType,
                a.PaymentId, a.Payment.Reference, a.IsActive, a.AllocatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<List<AllocationDto>> GetAllAllocationsAsync()
    {
        return await _db.Allocations
            .Include(a => a.User)
            .Include(a => a.Room)
            .Include(a => a.Payment)
            .OrderByDescending(a => a.AllocatedAt)
            .Select(a => new AllocationDto(
                a.Id, a.UserId, a.User.FullName, a.User.Email, a.User.MatricNumber,
                a.RoomId, a.Room.RoomNumber, a.Room.HostelBlock, a.Room.RoomType,
                a.PaymentId, a.Payment.Reference, a.IsActive, a.AllocatedAt))
            .ToListAsync();
    }

    public async Task<AllocationDto?> GetUserAllocationAsync(int userId)
    {
        return await _db.Allocations
            .Include(a => a.User)
            .Include(a => a.Room)
            .Include(a => a.Payment)
            .Where(a => a.UserId == userId && a.IsActive)
            .Select(a => new AllocationDto(
                a.Id, a.UserId, a.User.FullName, a.User.Email, a.User.MatricNumber,
                a.RoomId, a.Room.RoomNumber, a.Room.HostelBlock, a.Room.RoomType,
                a.PaymentId, a.Payment.Reference, a.IsActive, a.AllocatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeallocateAsync(int allocationId)
    {
        var allocation = await _db.Allocations
            .Include(a => a.Room)
            .FirstOrDefaultAsync(a => a.Id == allocationId);

        if (allocation == null) return false;

        allocation.IsActive = false;
        allocation.DeallocatedAt = DateTime.UtcNow;
        allocation.Room.OccupiedSlots = Math.Max(0, allocation.Room.OccupiedSlots - 1);

        await _db.SaveChangesAsync();
        return true;
    }
}

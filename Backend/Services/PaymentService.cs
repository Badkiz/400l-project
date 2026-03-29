using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HostelMS.Config;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Hubs;
using HostelMS.Interfaces;
using HostelMS.Models;

namespace HostelMS.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly PaystackSettings _paystack;
    private readonly IAllocationService _allocationService;
    private readonly IHubContext<RoomHub> _hub;
    private readonly ILogger<PaymentService> _logger;

    // FrontendOrigin is read from config; defaults to Live Server URL
    private readonly string _frontendOrigin;

    public PaymentService(
        AppDbContext db,
        PaystackSettings paystack,
        IAllocationService allocationService,
        IHubContext<RoomHub> hub,
        ILogger<PaymentService> logger,
        IConfiguration config)
    {
        _db = db;
        _paystack = paystack;
        _allocationService = allocationService;
        _hub = hub;
        _logger = logger;
        _frontendOrigin = config["FrontendOrigin"] ?? "http://127.0.0.1:5500";
    }

    public async Task<InitiatePaymentResponse> InitiatePaymentAsync(int userId, InitiatePaymentRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var room = await _db.Rooms.FindAsync(request.RoomId)
            ?? throw new InvalidOperationException("Room not found.");

        if (!room.IsActive)
            throw new InvalidOperationException("Room is not available.");

        if (room.AvailableSlots <= 0)
            throw new InvalidOperationException("Room is fully booked.");

        if (await _db.Allocations.AnyAsync(a => a.UserId == userId && a.IsActive))
            throw new InvalidOperationException("You already have an active room allocation.");

        // Generate a unique reference
        var reference = $"HMS-{userId}-{request.RoomId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // Save pending payment record
        var payment = new Payment
        {
            Reference  = reference,
            Status     = "Pending",
            Amount     = room.Price,
            UserId     = userId,
            RoomId     = request.RoomId
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Point to our fake Paystack page instead of real Paystack
        var mockUrl = $"{_frontendOrigin}/paystack-mock.html" +
                      $"?reference={reference}" +
                      $"&amount={room.Price}" +
                      $"&email={Uri.EscapeDataString(user.Email)}" +
                      $"&room={Uri.EscapeDataString(room.RoomNumber)}" +
                      $"&name={Uri.EscapeDataString(user.FullName)}";

        return new InitiatePaymentResponse(mockUrl, reference, room.Price);
    }

    // Called by the mock webhook endpoint (no HMAC needed in mock mode)
    public async Task<bool> VerifyAndProcessWebhookAsync(string payload, string signature)
    {
        PaystackWebhookPayload? webhookData;
        try
        {
            webhookData = JsonSerializer.Deserialize<PaystackWebhookPayload>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return false;
        }

        if (webhookData?.data == null) return true;

        var reference = webhookData.data.reference;

        var payment = await _db.Payments
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Reference == reference);

        if (payment == null) return true;

        // Idempotency check
        if (payment.Status == "Success") return true;

        if (webhookData.data.status != "success")
        {
            payment.Status = "Failed";
            await _db.SaveChangesAsync();
            return true;
        }

        payment.Status              = "Success";
        payment.PaystackTransactionId = webhookData.data.id;
        payment.VerifiedAt          = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Allocate the room
        try
        {
            var allocation = await _allocationService.AllocateRoomAsync(payment.UserId, payment.Id);
            if (allocation != null)
            {
                var room = await _db.Rooms.FindAsync(payment.RoomId);
                if (room != null)
                {
                    await _hub.Clients.All.SendAsync("RoomUpdated", new
                    {
                        roomId         = room.Id,
                        availableSlots = room.AvailableSlots,
                        occupiedSlots  = room.OccupiedSlots
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Allocation failed after payment {Ref}", reference);
        }

        return true;
    }

    // Also expose a direct "confirm payment" endpoint used by the mock page
    // so we don't need a real webhook infrastructure
    public async Task<bool> ConfirmMockPaymentAsync(string reference)
    {
        var payment = await _db.Payments
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Reference == reference);

        if (payment == null) return false;
        if (payment.Status == "Success") return true; // idempotent

        payment.Status              = "Success";
        payment.PaystackTransactionId = $"MOCK-{Guid.NewGuid():N}";
        payment.VerifiedAt          = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            var allocation = await _allocationService.AllocateRoomAsync(payment.UserId, payment.Id);
            if (allocation != null)
            {
                var room = await _db.Rooms.FindAsync(payment.RoomId);
                if (room != null)
                {
                    await _hub.Clients.All.SendAsync("RoomUpdated", new
                    {
                        roomId         = room.Id,
                        availableSlots = room.AvailableSlots,
                        occupiedSlots  = room.OccupiedSlots
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mock allocation failed for {Ref}", reference);
        }

        return true;
    }

    public async Task<List<PaymentDto>> GetUserPaymentsAsync(int userId)
    {
        return await _db.Payments
            .Include(p => p.Room)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id, p.Reference, p.Status, p.Amount,
                p.RoomId, p.Room.RoomNumber, p.CreatedAt, p.VerifiedAt))
            .ToListAsync();
    }

    public async Task<PaymentDto?> GetPaymentByReferenceAsync(string reference)
    {
        var p = await _db.Payments.Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Reference == reference);
        if (p == null) return null;
        return new PaymentDto(p.Id, p.Reference, p.Status, p.Amount,
            p.RoomId, p.Room.RoomNumber, p.CreatedAt, p.VerifiedAt);
    }
}

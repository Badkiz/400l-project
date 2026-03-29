namespace HostelMS.Models;

public class Payment
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Success | Failed
    public decimal Amount { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public string? PaystackTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }

    public User User { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public Allocation? Allocation { get; set; }
}

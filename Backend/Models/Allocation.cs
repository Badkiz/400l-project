namespace HostelMS.Models;

public class Allocation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public int PaymentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeallocatedAt { get; set; }

    public User User { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}

namespace HostelMS.Models;

public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string HostelBlock { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty; // Single | Double | Quad
    public int Capacity { get; set; }
    public int OccupiedSlots { get; set; } = 0;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int AvailableSlots => Capacity - OccupiedSlots;

    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
}

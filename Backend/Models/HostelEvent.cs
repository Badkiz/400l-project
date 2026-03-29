namespace HostelMS.Models;

public class HostelEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "official"; // official | safety | social | deadline
    public DateTime EventDate { get; set; }
    public string EventTime { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public User CreatedBy { get; set; } = null!;
}

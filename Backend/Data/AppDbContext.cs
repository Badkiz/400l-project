using Microsoft.EntityFrameworkCore;
using HostelMS.Models;

namespace HostelMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<HostelEvent> Events => Set<HostelEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.MatricNumber).IsUnique().HasFilter("[MatricNumber] IS NOT NULL");
            e.Property(u => u.Role).HasDefaultValue("Student");
        });

        // Room
        modelBuilder.Entity<Room>(e =>
        {
            e.HasIndex(r => new { r.RoomNumber, r.HostelBlock }).IsUnique();
            e.Property(r => r.Price).HasColumnType("decimal(18,2)");
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(p => p.Reference).IsUnique();
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.User)
             .WithMany(u => u.Payments)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Room)
             .WithMany()
             .HasForeignKey(p => p.RoomId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Allocation - prevent double booking at DB level
        modelBuilder.Entity<Allocation>(e =>
        {
            // One user can only have ONE active allocation
            e.HasIndex(a => new { a.UserId, a.IsActive })
             .HasFilter("[IsActive] = 1")
             .IsUnique();

            e.HasOne(a => a.User)
             .WithMany(u => u.Allocations)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Room)
             .WithMany(r => r.Allocations)
             .HasForeignKey(a => a.RoomId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Payment)
             .WithOne(p => p.Allocation)
             .HasForeignKey<Allocation>(a => a.PaymentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Message
        modelBuilder.Entity<Message>(e =>
        {
            e.HasOne(m => m.Sender)
             .WithMany(u => u.SentMessages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Receiver)
             .WithMany(u => u.ReceivedMessages)
             .HasForeignKey(m => m.ReceiverId)
             .OnDelete(DeleteBehavior.Restrict);
        });


        // Announcement
        modelBuilder.Entity<Announcement>(e =>
        {
            e.HasOne(a => a.CreatedBy)
             .WithMany()
             .HasForeignKey(a => a.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // HostelEvent
        modelBuilder.Entity<HostelEvent>(e =>
        {
            e.HasOne(ev => ev.CreatedBy)
             .WithMany()
             .HasForeignKey(ev => ev.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Admin is seeded at runtime in Program.cs (not here)
        // to avoid BCrypt generating a different hash on every migration
    }
}

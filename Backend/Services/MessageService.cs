using Microsoft.EntityFrameworkCore;
using HostelMS.Data;
using HostelMS.DTOs;
using HostelMS.Interfaces;
using HostelMS.Models;

namespace HostelMS.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MessageDto> SendMessageAsync(int senderId, SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Message text cannot be empty.");

        var receiver = await _db.Users.FindAsync(request.ReceiverId)
            ?? throw new InvalidOperationException("Recipient not found.");

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Text = request.Text.Trim()
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(senderId)!;
        return new MessageDto(message.Id, senderId, sender!.FullName,
            request.ReceiverId, receiver.FullName, message.Text, message.Timestamp, false);
    }

    public async Task<List<MessageDto>> GetConversationAsync(int userId1, int userId2)
    {
        return await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m =>
                (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderBy(m => m.Timestamp)
            .Select(m => new MessageDto(
                m.Id, m.SenderId, m.Sender.FullName,
                m.ReceiverId, m.Receiver.FullName,
                m.Text, m.Timestamp, m.IsRead))
            .ToListAsync();
    }

    public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
    {
        // Get all users this person has messaged or received messages from
        var partnerIds = await _db.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

        var conversations = new List<ConversationDto>();

        foreach (var partnerId in partnerIds)
        {
            var partner = await _db.Users.FindAsync(partnerId);
            if (partner == null) continue;

            var lastMsg = await _db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == partnerId) ||
                    (m.SenderId == partnerId && m.ReceiverId == userId))
                .OrderByDescending(m => m.Timestamp)
                .Select(m => new MessageDto(
                    m.Id, m.SenderId, m.Sender.FullName,
                    m.ReceiverId, m.Receiver.FullName,
                    m.Text, m.Timestamp, m.IsRead))
                .FirstOrDefaultAsync();

            var unread = await _db.Messages
                .CountAsync(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead);

            conversations.Add(new ConversationDto(
                partner.Id, partner.FullName, partner.Email, partner.Role, lastMsg, unread));
        }

        return conversations.OrderByDescending(c => c.LastMessage?.Timestamp).ToList();
    }

    public async Task MarkAsReadAsync(int senderId, int receiverId)
    {
        var unread = await _db.Messages
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
            .ToListAsync();

        unread.ForEach(m => m.IsRead = true);
        await _db.SaveChangesAsync();
    }
}

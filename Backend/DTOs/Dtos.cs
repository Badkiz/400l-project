namespace HostelMS.DTOs;

// ─── Auth ───────────────────────────────────────────────
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? MatricNumber,
    string? PhoneNumber
);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    int UserId,
    string FullName,
    string Email,
    string Role
);

// ─── Room ───────────────────────────────────────────────
public record RoomDto(
    int Id,
    string RoomNumber,
    string HostelBlock,
    string RoomType,
    int Capacity,
    int OccupiedSlots,
    int AvailableSlots,
    decimal Price,
    string? Description,
    bool IsActive
);

public record CreateRoomRequest(
    string RoomNumber,
    string HostelBlock,
    string RoomType,
    int Capacity,
    decimal Price,
    string? Description
);

public record UpdateRoomRequest(
    string? RoomNumber,
    string? HostelBlock,
    string? RoomType,
    int? Capacity,
    decimal? Price,
    string? Description,
    bool? IsActive
);

// ─── Payment ─────────────────────────────────────────────
public record InitiatePaymentRequest(int RoomId);

public record InitiatePaymentResponse(
    string AuthorizationUrl,
    string Reference,
    decimal Amount
);

public record PaymentDto(
    int Id,
    string Reference,
    string Status,
    decimal Amount,
    int RoomId,
    string RoomNumber,
    DateTime CreatedAt,
    DateTime? VerifiedAt
);

// ─── Paystack Webhook ─────────────────────────────────────
public record PaystackWebhookPayload(
    string @event,
    PaystackWebhookData data
);

public record PaystackWebhookData(
    string reference,
    string status,
    decimal amount,
    string id
);

// ─── Allocation ──────────────────────────────────────────
public record AllocationDto(
    int Id,
    int UserId,
    string StudentName,
    string StudentEmail,
    string? MatricNumber,
    int RoomId,
    string RoomNumber,
    string HostelBlock,
    string RoomType,
    int PaymentId,
    string PaymentReference,
    bool IsActive,
    DateTime AllocatedAt
);

// ─── Message ─────────────────────────────────────────────
public record SendMessageRequest(int ReceiverId, string Text);

public record MessageDto(
    int Id,
    int SenderId,
    string SenderName,
    int ReceiverId,
    string ReceiverName,
    string Text,
    DateTime Timestamp,
    bool IsRead
);

public record ConversationDto(
    int UserId,
    string UserName,
    string UserEmail,
    string Role,
    MessageDto? LastMessage,
    int UnreadCount
);

public record MockConfirmRequest(string Reference);

// ─── Announcements ───────────────────────────────────────
public record AnnouncementDto(
    int Id,
    string Title,
    string Body,
    string Category,
    bool IsPinned,
    string CreatedBy,
    DateTime CreatedAt
);

public record CreateAnnouncementRequest(
    string Title,
    string Body,
    string Category,
    bool IsPinned
);

// ─── Events ──────────────────────────────────────────────
public record EventDto(
    int Id,
    string Title,
    string Description,
    string Category,
    DateTime EventDate,
    string EventTime,
    string CreatedBy,
    DateTime CreatedAt
);

public record CreateEventRequest(
    string Title,
    string Description,
    string Category,
    DateTime EventDate,
    string EventTime
);

// ─── Profile / Settings ──────────────────────────────────
public record UpdateProfileRequest(
    string? FullName,
    string? PhoneNumber
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ProfileDto(
    int Id,
    string FullName,
    string Email,
    string? MatricNumber,
    string? PhoneNumber,
    string Role,
    DateTime CreatedAt
);

// ─── Student list (for admin) ─────────────────────────────
public record StudentDto(
    int Id,
    string FullName,
    string Email,
    string? MatricNumber,
    string? PhoneNumber,
    DateTime CreatedAt,
    bool HasAllocation
);

// ─── Student Detail (admin view) ─────────────────────────
public record StudentDetailDto(
    int Id,
    string FullName,
    string Email,
    string? MatricNumber,
    string? PhoneNumber,
    DateTime CreatedAt,
    bool HasAllocation,
    AllocationDto? Allocation,
    List<PaymentDto> Payments
);

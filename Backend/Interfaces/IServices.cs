using HostelMS.DTOs;
using HostelMS.Models;

namespace HostelMS.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    string GenerateToken(User user);
}

public interface IRoomService
{
    Task<List<RoomDto>> GetAllRoomsAsync(bool activeOnly = true);
    Task<RoomDto?> GetRoomByIdAsync(int id);
    Task<RoomDto> CreateRoomAsync(CreateRoomRequest request);
    Task<RoomDto?> UpdateRoomAsync(int id, UpdateRoomRequest request);
    Task<bool> DeleteRoomAsync(int id);
}

public interface IPaymentService
{
    Task<InitiatePaymentResponse> InitiatePaymentAsync(int userId, InitiatePaymentRequest request);
    Task<bool> VerifyAndProcessWebhookAsync(string payload, string signature);
    Task<List<PaymentDto>> GetUserPaymentsAsync(int userId);
    Task<PaymentDto?> GetPaymentByReferenceAsync(string reference);
    Task<bool> ConfirmMockPaymentAsync(string reference);
}

public interface IAllocationService
{
    Task<AllocationDto?> AllocateRoomAsync(int userId, int paymentId);
    Task<List<AllocationDto>> GetAllAllocationsAsync();
    Task<AllocationDto?> GetUserAllocationAsync(int userId);
    Task<bool> DeallocateAsync(int allocationId);
}

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int senderId, SendMessageRequest request);
    Task<List<MessageDto>> GetConversationAsync(int userId1, int userId2);
    Task<List<ConversationDto>> GetConversationsAsync(int userId);
    Task MarkAsReadAsync(int senderId, int receiverId);
}

public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetAllAsync();
    Task<AnnouncementDto> CreateAsync(int adminId, CreateAnnouncementRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> TogglePinAsync(int id);
}

public interface IEventService
{
    Task<List<EventDto>> GetAllAsync();
    Task<EventDto> CreateAsync(int adminId, CreateEventRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(int userId);
    Task<ProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<List<StudentDto>> GetAllStudentsAsync();
    Task<StudentDetailDto?> GetStudentDetailAsync(int studentId);
}

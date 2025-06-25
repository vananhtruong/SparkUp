using SparkUp.Business;
using SparkUp.MVC.Models;

namespace SparkUp.MVC.Service
{    public interface IChatService
    {
        // Chat Room Management
        Task<ChatRoom?> GetOrCreateChatRoomAsync(int taskId);
        Task<ChatRoom?> CreateChatRoomAsync(int taskId);
        Task<ChatRoom?> GetChatRoomByIdAsync(int chatRoomId);
        Task<ChatRoom?> GetChatRoomByTaskIdAsync(int taskId);
        Task<bool> IsChatAvailableForTaskAsync(int taskId, int userId);
        Task<ChatRoom?> UpdateChatRoomStatusAsync(int chatRoomId, ChatRoomStatus status);
        
        // Message Management
        Task<ChatMessage> SendMessageAsync(int chatRoomId, int senderId, string content, ChatMessageType messageType = ChatMessageType.Text);
        Task<ChatMessage> SendSystemMessageAsync(int chatRoomId, string content);
        Task<List<ChatMessage>> GetChatMessagesAsync(int chatRoomId, int page = 1, int pageSize = 50);
        Task<ChatMessage?> GetMessageByIdAsync(int messageId);
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        Task<bool> MarkAllMessagesAsReadAsync(int chatRoomId, int userId);
        Task<int> GetUnreadMessageCountAsync(int chatRoomId, int userId);
        
        // Dispute Management
        Task<ChatRoom?> InitiateDisputeAsync(int chatRoomId, int initiatorId, string reason);
        Task<ChatRoom?> AssignAdminToChatAsync(int chatRoomId, int adminId);
        Task<DisputeResolution> ResolveDisputeAsync(int chatRoomId, int adminId, string resolution, string actionTaken, decimal? refundAmount = null, string? notes = null);
        Task<List<DisputeResolution>> GetDisputeHistoryAsync(int chatRoomId);
        
        // Admin Management
        Task<List<ChatRoom>> GetPendingDisputesAsync();
        Task<List<ChatRoom>> GetChatRoomsForAdminAsync(int adminId);
        
        // User Access Control
        Task<bool> CanUserAccessChatAsync(int chatRoomId, int userId);
        Task<string> GetUserRoleInChatAsync(int chatRoomId, int userId);
        
        // Chat Statistics
        Task<int> GetActiveChatCountAsync();
        Task<int> GetDisputedChatCountAsync();
        Task<ChatStatisticsViewModel> GetChatStatisticsAsync();
          // Get all chat rooms for a specific user
        Task<List<ChatRoomViewModel>> GetUserChatRoomsAsync(int userId);
    }
}

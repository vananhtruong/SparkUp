using SparkUp.Business;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SparkUp.MVC.Service
{
    public interface IChatService
    {
        /// <summary>
        /// Create a new support chat session
        /// </summary>
        Task<SupportChatSession> CreateChatSessionAsync(int customerId, string subject);

        /// <summary>
        /// Get customer's active chat sessions
        /// </summary>
        Task<List<SupportChatSession>> GetCustomerChatSessionsAsync(int customerId);

        /// <summary>
        /// Get all chat sessions for admin
        /// </summary>
        Task<List<SupportChatSession>> GetAllChatSessionsAsync(string status = "all");

        /// <summary>
        /// Get a specific chat session with messages
        /// </summary>
        Task<SupportChatSession?> GetChatSessionAsync(int sessionId, bool includeMessages = true);

        /// <summary>
        /// Assign admin to a chat session
        /// </summary>
        Task<bool> AssignAdminToChatAsync(int sessionId, int adminId);

        /// <summary>
        /// Send a message in chat session
        /// </summary>
        Task<SupportChatMessage> SendMessageAsync(int sessionId, int senderId, string messageText, string messageType = "Text");

        /// <summary>
        /// Update chat session status
        /// </summary>
        Task<bool> UpdateChatStatusAsync(int sessionId, string status);

        /// <summary>
        /// Update online status
        /// </summary>
        Task<bool> UpdateOnlineStatusAsync(int sessionId, int userId, bool isOnline);

        /// <summary>
        /// Mark messages as read
        /// </summary>
        Task<bool> MarkMessagesAsReadAsync(int sessionId, int readerId);

        /// <summary>
        /// Get unread message count for user
        /// </summary>
        Task<int> GetUnreadMessageCountAsync(int userId);
    }
}
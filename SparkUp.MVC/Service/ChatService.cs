using Microsoft.EntityFrameworkCore;
using SparkUp.Business;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SparkUp.MVC.Service
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SupportChatSession> CreateChatSessionAsync(int customerId, string subject)
        {
            var session = new SupportChatSession
            {
                CustomerId = customerId,
                Subject = subject,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                IsCustomerOnline = true,
                IsAdminOnline = false
            };

            _context.SupportChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<List<SupportChatSession>> GetCustomerChatSessionsAsync(int customerId)
        {
            return await _context.SupportChatSessions
                .Where(s => s.CustomerId == customerId)
                .Include(s => s.Customer)
                .Include(s => s.Admin)
                .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SupportChatSession>> GetAllChatSessionsAsync(string status = "all")
        {
            var query = _context.SupportChatSessions
                .Include(s => s.Customer)
                .Include(s => s.Admin)
                .AsQueryable();

            if (status != "all")
            {
                query = query.Where(s => s.Status == status);
            }

            return await query
                .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                .ToListAsync();
        }

        public async Task<SupportChatSession?> GetChatSessionAsync(int sessionId, bool includeMessages = true)
        {
            var query = _context.SupportChatSessions
                .Include(s => s.Customer)
                .Include(s => s.Admin)
                .AsQueryable();

            if (includeMessages)
            {
                query = query.Include(s => s.Messages)
                    .ThenInclude(m => m.Sender);
            }

            return await query.FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<bool> AssignAdminToChatAsync(int sessionId, int adminId)
        {
            var session = await _context.SupportChatSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.AdminId = adminId;
            if (session.Status == "Open")
            {
                session.Status = "InProgress";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SupportChatMessage> SendMessageAsync(int sessionId, int senderId, string messageText, string messageType = "Text")
        {
            var message = new SupportChatMessage
            {
                SessionId = sessionId,
                SenderId = senderId,
                MessageText = messageText,
                MessageType = messageType,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.SupportChatMessages.Add(message);

            // Update session last message time
            var session = await _context.SupportChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastMessageAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Load the message with sender info
            return await _context.SupportChatMessages
                .Include(m => m.Sender)
                .FirstAsync(m => m.Id == message.Id);
        }

        public async Task<bool> UpdateChatStatusAsync(int sessionId, string status)
        {
            var session = await _context.SupportChatSessions.FindAsync(sessionId);
            if (session == null) return false;

            session.Status = status;
            if (status == "Closed" || status == "Resolved")
            {
                session.ClosedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateOnlineStatusAsync(int sessionId, int userId, bool isOnline)
        {
            var session = await _context.SupportChatSessions.FindAsync(sessionId);
            if (session == null) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (user.Role == "CUSTOMER" && session.CustomerId == userId)
            {
                session.IsCustomerOnline = isOnline;
            }
            else if (user.Role == "ADMIN" && session.AdminId == userId)
            {
                session.IsAdminOnline = isOnline;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkMessagesAsReadAsync(int sessionId, int readerId)
        {
            var messages = await _context.SupportChatMessages
                .Where(m => m.SessionId == sessionId && m.SenderId != readerId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return 0;

            if (user.Role == "CUSTOMER")
            {
                return await _context.SupportChatMessages
                    .Join(_context.SupportChatSessions,
                        m => m.SessionId,
                        s => s.Id,
                        (m, s) => new { Message = m, Session = s })
                    .Where(x => x.Session.CustomerId == userId && 
                               x.Message.SenderId != userId && 
                               !x.Message.IsRead)
                    .CountAsync();
            }
            else if (user.Role == "ADMIN")
            {
                return await _context.SupportChatMessages
                    .Join(_context.SupportChatSessions,
                        m => m.SessionId,
                        s => s.Id,
                        (m, s) => new { Message = m, Session = s })
                    .Where(x => (x.Session.AdminId == userId || x.Session.AdminId == null) && 
                               x.Message.SenderId != userId && 
                               !x.Message.IsRead)
                    .CountAsync();
            }

            return 0;
        }
    }
}
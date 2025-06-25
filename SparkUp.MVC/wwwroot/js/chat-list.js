// Chat List Management
class ChatListManager {
    constructor() {
        this.chats = [];
        this.currentChatId = null;
        this.init();
    }

    async init() {
        try {
            await this.loadChatRooms();
            this.setupMobileSidebar();
        } catch (error) {
            console.error('Failed to initialize chat list:', error);
        }
    }    async loadChatRooms() {
        try {
            // Try different potential API endpoints
            const endpoints = [
                '/GetChatRooms',
                '/Chat/GetChatRooms',
                '/api/chat/rooms'
            ];
            
            let response = null;
            let success = false;
            let lastStatus = null;
            
            console.log('Starting to load chat rooms');
            
            for (const endpoint of endpoints) {
                try {
                    console.log(`Trying to load chat rooms from: ${endpoint}`);
                    response = await fetch(endpoint);
                    
                    console.log(`Response from ${endpoint}:`, response.status, response.statusText);
                    
                    if (response.ok) {
                        success = true;
                        console.log('Successfully loaded chat rooms from:', endpoint);
                        break;
                    } else {
                        lastStatus = `${response.status}: ${response.statusText}`;
                    }
                } catch (err) {
                    console.warn(`Failed to load from ${endpoint}:`, err);
                }
            }
            
            if (success && response) {
                const data = await response.json();
                console.log('Chat rooms data received:', data);
                this.chats = Array.isArray(data) ? data : [];
                this.renderChatRooms();
            } else {
                console.error('Failed to load chat rooms:', lastStatus);
                document.getElementById('chatRoomsList').innerHTML = 
                    '<div class="text-center text-danger p-3">Không thể tải danh sách cuộc trò chuyện.</div>';
            }
        } catch (error) {
            console.error('Error loading chat rooms:', error);
            document.getElementById('chatRoomsList').innerHTML = 
                '<div class="text-center text-danger p-3">Không thể tải danh sách cuộc trò chuyện.</div>';
        } finally {
            // Hide loading spinner
            const loadingElement = document.getElementById('loading-rooms');
            if (loadingElement) {
                loadingElement.remove();
            }
        }
    }

    renderChatRooms() {
        const container = document.getElementById('chatRoomsList');
        if (!container) return;

        // Clear loading indicator
        container.innerHTML = '';

        if (!this.chats || !this.chats.length) {
            container.innerHTML = '<div class="text-center text-muted p-3">Không có cuộc trò chuyện nào.</div>';
            return;
        }

        // Get current chat room ID from URL or config
        this.currentChatId = this.getCurrentChatRoomId();

        // Create room elements
        this.chats.forEach(chat => {
            const chatElement = this.createChatRoomElement(chat);
            container.appendChild(chatElement);
        });
    }    createChatRoomElement(chat) {
        const div = document.createElement('div');
        div.className = `chat-room-item ${chat.id == this.currentChatId ? 'active' : ''} ${chat.unreadCount > 0 ? 'unread' : ''}`;
        div.setAttribute('data-chat-id', chat.id);
        
        // Determine who is the other user (customer or worker)
        const otherUser = chat.userRole === 'Customer' ? chat.workerName : chat.customerName;
        
        // Format time
        const time = this.formatTime(chat.lastMessageAt);
        
        // Determine status icon and class
        const statusInfo = this.getStatusInfo(chat.status);
        
        div.innerHTML = `
            <div class="d-flex justify-content-between align-items-start">
                <div class="flex-grow-1">
                    <div class="d-flex align-items-center justify-content-between mb-2">
                        <div class="name">${chat.taskTitle || 'Công việc #' + chat.taskId}</div>
                        <span class="badge ${statusInfo.class}" style="font-size: 0.65rem;">
                            <i class="${statusInfo.icon} me-1"></i>${statusInfo.text}
                        </span>
                    </div>
                    <div class="last-message">
                        <i class="fas fa-user me-1"></i>
                        <span class="partner-name">${otherUser}</span>: 
                        <span class="message-preview">${chat.lastMessage || 'Chưa có tin nhắn'}</span>
                    </div>
                    <div class="meta mt-2">
                        <small class="time">
                            <i class="fas fa-clock me-1"></i>${time}
                        </small>
                        ${chat.unreadCount > 0 ? `
                        <div class="unread-badge">
                            <i class="fas fa-envelope me-1"></i>${chat.unreadCount}
                        </div>` : ''}
                    </div>
                </div>
            </div>
        `;
        
        div.addEventListener('click', () => this.handleChatRoomClick(chat));
        
        return div;
    }handleChatRoomClick(chat) {
        try {
            if (!chat || !chat.id) {
                console.error('Invalid chat room data', chat);
                return;
            }
            
            // Lưu ID phòng chat đã chọn vào sessionStorage
            sessionStorage.setItem('selectedChatRoom', chat.id);
            
            // Log thông tin phòng chat được click
            console.log(`Clicked on chat room: ${chat.id}, TaskId: ${chat.taskId}, Title: ${chat.taskTitle}`);
            
            // Kiểm tra nếu đã ở trang Chat/Room/{id} tương ứng
            const currentPath = window.location.pathname;
            const targetPath = `/Chat/Room/${chat.id}`;
            
            if (currentPath === targetPath) {
                console.log(`Already on chat room ${chat.id}, reloading content without page refresh`);
                
                // Nếu đã ở đúng trang và có window.chatManager, reload nội dung mà không reload trang
                if (window.chatManager) {
                    window.chatManager.loadMessages()
                        .then(() => {
                            console.log('Chat content reloaded without page refresh');
                            // Đánh dấu tất cả tin nhắn là đã đọc
                            return window.chatManager.markAllMessagesAsRead();
                        })
                        .catch(err => {
                            console.error('Error reloading chat content:', err);
                        });
                        
                    // Cập nhật UI để đánh dấu phòng chat đã chọn
                    document.querySelectorAll('.chat-room-item').forEach(item => {
                        item.classList.remove('active');
                    });
                    
                    const activeChatElement = document.querySelector(`.chat-room-item[data-chat-id="${chat.id}"]`);
                    if (activeChatElement) {
                        activeChatElement.classList.add('active');
                        activeChatElement.classList.remove('unread');
                        
                        // Xóa badge unread count
                        const unreadBadge = activeChatElement.querySelector('.unread-badge');
                        if (unreadBadge) {
                            unreadBadge.style.display = 'none';
                        }
                    }
                } else {
                    console.log('Chat manager not found, reloading page');
                    window.location.href = targetPath;
                }
            } else {
                // Nếu không ở đúng trang, chuyển hướng đến trang mới
                console.log(`Navigating to chat room: ${targetPath}`);
                window.location.href = targetPath;
            }
        } catch (error) {
            console.error('Error handling chat room click:', error);
        }
    }

    getCurrentChatRoomId() {
        // Try to get from window.chatConfig first (set in Room view)
        if (window.chatConfig && window.chatConfig.chatRoomId) {
            return window.chatConfig.chatRoomId;
        }
        
        // Otherwise try to extract from URL
        const path = window.location.pathname;
        const matches = path.match(/\/Chat\/Room\/(\d+)/i);
        if (matches && matches[1]) {
            return parseInt(matches[1]);
        }
        
        return null;
    }    setupMobileSidebar() {
        const toggleButton = document.getElementById('toggleSidebar');
        const closeSidebar = document.getElementById('closeSidebar');
        const sidebar = document.getElementById('chatSidebar');
        const backdrop = document.getElementById('sidebarBackdrop');
        
        // Show sidebar
        if (toggleButton && sidebar) {
            toggleButton.addEventListener('click', () => {
                sidebar.classList.add('show');
                backdrop.classList.add('show');
            });
        }
        
        // Hide sidebar
        if (closeSidebar && sidebar) {
            closeSidebar.addEventListener('click', () => {
                sidebar.classList.remove('show');
                backdrop.classList.remove('show');
            });
        }
        
        // Hide when clicking backdrop
        if (backdrop && sidebar) {
            backdrop.addEventListener('click', () => {
                sidebar.classList.remove('show');
                backdrop.classList.remove('show');
            });
        }
    }

    formatTime(dateString) {
        if (!dateString) return '';
        
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return '';
        
        const now = new Date();
        const isToday = now.toDateString() === date.toDateString();
        
        if (isToday) {
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        } else {
            return date.toLocaleDateString();
        }
    }
      getStatusInfo(status) {
        const statusMap = {
            'Active': {
                class: 'bg-success',
                icon: 'fas fa-check-circle',
                text: 'Hoạt động'
            },
            'Disputed': {
                class: 'bg-danger',
                icon: 'fas fa-exclamation-triangle',
                text: 'Tranh chấp'
            },
            'Completed': {
                class: 'bg-info',
                icon: 'fas fa-check-double',
                text: 'Hoàn thành'
            },
            'Closed': {
                class: 'bg-secondary',
                icon: 'fas fa-times-circle',
                text: 'Đã đóng'
            }
        };
        
        return statusMap[status] || {
            class: 'bg-secondary',
            icon: 'fas fa-question-circle',
            text: status || 'Không xác định'
        };
    }
}

// Initialize chat list when page loads
document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('chatRoomsList')) {
        window.chatListManager = new ChatListManager();
    }
});

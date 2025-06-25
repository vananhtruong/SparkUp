// Chat JavaScript with SignalR Integration

class ChatManager {
    constructor(config) {
        this.config = config;
        this.connection = null;
        this.typingTimer = null;
        this.isTyping = false;
        this.messages = [];
        
        this.init();
    }

    async init() {
        try {
            await this.initSignalR();
            this.initUI();
            await this.loadMessages();
            this.startConnection();
        } catch (error) {
            console.error('Failed to initialize chat:', error);
            this.showError('Failed to connect to chat. Please refresh the page.');
        }
    }

    async initSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub")
            .withAutomaticReconnect()
            .build();

        // Handle incoming messages
        this.connection.on("ReceiveMessage", (message) => {
            this.addMessage(message);
        });

        // Handle user typing
        this.connection.on("UserTyping", (data) => {
            this.showTypingIndicator(data.UserName, data.IsTyping);
        });

        // Handle message read status
        this.connection.on("MessageRead", (data) => {
            this.markMessageAsRead(data.MessageId);
        });

        // Handle dispute events
        this.connection.on("DisputeInitiated", (data) => {
            this.handleDisputeInitiated(data);
        });

        this.connection.on("DisputeResolved", (data) => {
            this.handleDisputeResolved(data);
        });

        // Handle connection status
        this.connection.onreconnecting(() => {
            this.showConnectionStatus("Reconnecting to chat server...", "warning");
        });

        this.connection.onreconnected(() => {
            this.showConnectionStatus("Reconnected to chat server!", "success");
        });

        this.connection.onclose(() => {
            this.showConnectionStatus("Disconnected from chat server. Please refresh the page.", "danger");
        });
    }

    initUI() {
        // Initialize UI elements
        this.chatMessagesElement = document.getElementById('chat-messages');
        this.messageInputElement = document.getElementById('message-input');
        this.sendButtonElement = document.getElementById('send-button');
        this.chatStatusElement = document.getElementById('chat-status');
        this.typingIndicatorElement = document.getElementById('typing-indicator');

        // If any of these elements don't exist, show an error
        if (!this.chatMessagesElement || !this.messageInputElement || !this.sendButtonElement) {
            console.error('Required chat UI elements not found');
            return;
        }

        // Set up event listeners
        this.messageInputElement.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                this.sendMessage();
            }
        });

        this.messageInputElement.addEventListener('input', () => {
            this.handleTypingEvent();
        });

        this.sendButtonElement.addEventListener('click', () => {
            this.sendMessage();
        });

        // Add a scroll event listener to mark messages as read when scrolled into view
        this.chatMessagesElement.addEventListener('scroll', () => {
            this.checkVisibleMessages();
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log('Connected to SignalR hub');
            
            // Join the chat room group
            await this.connection.invoke('JoinRoom', this.config.chatRoomId);
            
            // Mark all messages as read when connection starts
            await this.markAllMessagesAsRead();
            
        } catch (error) {
            console.error('Failed to connect to SignalR hub:', error);
            setTimeout(() => this.startConnection(), 5000);
        }
    }

    async loadMessages() {
        if (!this.config || !this.config.chatRoomId) {
            console.error('Chat configuration is missing or invalid');
            this.showError('Invalid chat configuration. Please refresh the page.');
            return;
        }
        
        try {
            console.log('Loading messages for chat room:', this.config.chatRoomId);
            
            // Create an array of endpoints to try in order
            const endpoints = [
                `/Chat/GetMessages?chatRoomId=${this.config.chatRoomId}&page=1&pageSize=50`,
                `/Chat/GetMessages/${this.config.chatRoomId}?page=1&pageSize=50`,
                `/Chat/Api/Messages/${this.config.chatRoomId}?page=1&pageSize=50`,
                `/api/chat/messages?roomId=${this.config.chatRoomId}&page=1&pageSize=50`
            ];
            
            let response = null;
            let lastError = null;
            
            // Try each endpoint until one works
            for (const endpoint of endpoints) {
                try {
                    console.log(`Trying endpoint: ${endpoint}`);
                    response = await fetch(endpoint);
                    
                    if (response.ok) {
                        break;
                    }
                } catch (error) {
                    lastError = error;
                    console.error(`Failed to fetch messages from ${endpoint}:`, error);
                }
            }
            
            // If no endpoint worked, show error and return
            if (!response || !response.ok) {
                console.error('All message loading attempts failed:', lastError);
                this.showError('Could not load messages. Please refresh the page.');
                return;
            }
            
            const messages = await response.json();
            console.log(`Loaded ${messages.length} messages`);
            
            this.messages = messages;
            this.displayMessages(messages);
            
            // Mark all messages as read
            this.markAllMessagesAsRead();
            
        } catch (error) {
            console.error('Error loading messages:', error);
            this.showError('Could not load messages. Please refresh the page.');
        }
    }
    
    async markAllMessagesAsRead() {
        try {
            await fetch(`/Chat/MarkAllAsRead/${this.config.chatRoomId}`, {
                method: 'POST'
            });
            console.log('Marked all messages as read');
        } catch (error) {
            console.error('Failed to mark all messages as read:', error);
        }
    }
    
    displayMessages(messages) {
        if (!this.chatMessagesElement) return;
        
        // Clear existing messages
        this.chatMessagesElement.innerHTML = '';
        
        if (!messages || messages.length === 0) {
            const noMessagesEl = document.createElement('div');
            noMessagesEl.className = 'text-center text-muted p-3';
            noMessagesEl.textContent = 'No messages yet. Start a conversation!';
            this.chatMessagesElement.appendChild(noMessagesEl);
            return;
        }
        
        // Add each message to the DOM
        messages.forEach(message => {
            this.addMessageToDOM(message, false);
        });
        
        // Scroll to the bottom
        this.scrollToBottom();
    }
    
    sendMessage() {
        const content = this.messageInputElement.value.trim();
        
        if (!content) return;
        
        const message = {
            chatRoomId: this.config.chatRoomId,
            senderId: this.config.currentUserId,
            content: content,
            messageType: 'Text'
        };
        
        // Clear the input field
        this.messageInputElement.value = '';
        
        // Reset typing indicator
        this.isTyping = false;
        clearTimeout(this.typingTimer);
        
        // Send message to server
        this.connection.invoke('SendMessage', message)
            .catch(error => {
                console.error('Error sending message:', error);
                this.showError('Failed to send message. Please try again.');
            });
    }
    
    addMessage(message) {
        // Add to messages array
        this.messages.push(message);
        
        // Add to DOM
        this.addMessageToDOM(message, true);
        
        // Scroll to bottom
        this.scrollToBottom();
        
        // If the message is from someone else, mark as read
        if (message.senderId !== this.config.currentUserId) {
            this.markAsRead(message.id);
        }
    }
    
    addMessageToDOM(message, animate = false) {
        if (!this.chatMessagesElement) return;
        
        const messageElement = this.createMessageElement(message);
        
        if (animate) {
            messageElement.classList.add('new-message-animation');
        }
        
        this.chatMessagesElement.appendChild(messageElement);
    }
    
    createMessageElement(message) {
        const isCurrentUser = message.senderId == this.config.currentUserId;
        const messageContainer = document.createElement('div');
        messageContainer.className = `message-container ${isCurrentUser ? 'outgoing' : 'incoming'}`;
        messageContainer.dataset.messageId = message.id;
        
        // Create the message content element
        let messageContent = '';
        
        // Check message type
        if (message.messageType === 'System' || message.messageType === 'DisputeInitiated' || message.messageType === 'DisputeResolved') {
            // System message
            messageContainer.className = 'system-message';
            messageContent = `
                <div class="alert alert-info mb-2">
                    <i class="bi bi-info-circle"></i> 
                    ${this.escapeHtml(message.content)}
                </div>
            `;
        } else if (message.messageType === 'File') {
            // File message
            messageContent = `
                <div class="message ${isCurrentUser ? 'sent' : 'received'}">
                    <div class="message-sender">${message.senderName || 'Unknown'}</div>
                    <div class="message-file">
                        <i class="bi bi-file-earmark"></i>
                        <a href="/Chat/DownloadFile/${message.id}" target="_blank">${message.content}</a>
                    </div>
                    <div class="message-time">${this.formatTime(message.sentAt)}</div>
                    <div class="message-status">${message.isRead ? '<i class="bi bi-check-all"></i>' : '<i class="bi bi-check"></i>'}</div>
                </div>
            `;
        } else {
            // Regular text message
            messageContent = `
                <div class="message ${isCurrentUser ? 'sent' : 'received'}">
                    <div class="message-sender">${message.senderName || 'Unknown'}</div>
                    <div class="message-content">${this.escapeHtml(message.content)}</div>
                    <div class="message-time">${this.formatTime(message.sentAt)}</div>
                    <div class="message-status">${message.isRead ? '<i class="bi bi-check-all"></i>' : '<i class="bi bi-check"></i>'}</div>
                </div>
            `;
        }
        
        messageContainer.innerHTML = messageContent;
        return messageContainer;
    }
    
    addSystemMessage(content) {
        const message = {
            id: `sys_${Date.now()}`,
            senderId: null,
            senderName: 'System',
            content: content,
            sentAt: new Date().toISOString(),
            isRead: true,
            messageType: 'System'
        };
        
        this.addMessageToDOM(message, true);
        this.scrollToBottom();
    }
    
    async markAsRead(messageId) {
        try {
            await fetch(`/Chat/MarkAsRead/${messageId}`, {
                method: 'POST'
            });
            
            this.markMessageAsRead(messageId);
            
            // Notify other clients
            await this.connection.invoke('MarkMessageAsRead', {
                messageId: messageId,
                chatRoomId: this.config.chatRoomId,
                userId: this.config.currentUserId
            });
        } catch (error) {
            console.error('Failed to mark message as read:', error);
        }
    }
    
    checkVisibleMessages() {
        // Check if messages are visible and mark them as read
        const messages = this.chatMessagesElement.querySelectorAll('.message-container.incoming');
        
        messages.forEach(messageEl => {
            const rect = messageEl.getBoundingClientRect();
            const isVisible = rect.top >= 0 && rect.bottom <= window.innerHeight;
            
            if (isVisible) {
                const messageId = messageEl.dataset.messageId;
                this.markAsRead(messageId);
            }
        });
    }
    
    markMessageAsRead(messageId) {
        // Update the message in the DOM
        const messageElement = this.chatMessagesElement.querySelector(`[data-message-id="${messageId}"]`);
        if (messageElement) {
            const statusEl = messageElement.querySelector('.message-status');
            if (statusEl) {
                statusEl.innerHTML = '<i class="bi bi-check-all"></i>';
            }
        }
    }
    
    showTypingIndicator(userName, isTyping) {
        if (!this.typingIndicatorElement) return;
        
        if (isTyping) {
            this.typingIndicatorElement.textContent = `${userName} is typing...`;
            this.typingIndicatorElement.style.display = 'block';
        } else {
            this.typingIndicatorElement.style.display = 'none';
        }
    }
    
    showConnectionStatus(message, type) {
        if (!this.chatStatusElement) return;
        
        this.chatStatusElement.innerHTML = `<div class="alert alert-${type} mb-0">${message}</div>`;
        this.chatStatusElement.style.display = 'block';
    }
    
    showError(message) {
        if (!this.chatStatusElement) return;
        
        console.error('Chat error:', message);
        
        // Show error in status element
        this.chatStatusElement.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-circle"></i> ${message}
            </div>
        `;
        this.chatStatusElement.style.display = 'block';
        
        // Hide after 5 seconds
        setTimeout(() => {
            this.chatStatusElement.style.display = 'none';
        }, 5000);
    }
    
    handleTypingEvent() {
        if (!this.isTyping) {
            this.isTyping = true;
            
            // Notify others that this user is typing
            this.connection.invoke('UserTyping', {
                chatRoomId: this.config.chatRoomId,
                userId: this.config.currentUserId,
                userName: this.config.userName,
                isTyping: true
            }).catch(err => console.error('Error sending typing notification:', err));
        }
        
        // Clear existing timer
        clearTimeout(this.typingTimer);
        
        // Set a timer to stop typing indicator after 1.5 seconds of inactivity
        this.typingTimer = setTimeout(() => {
            this.isTyping = false;
            
            // Notify others that this user stopped typing
            this.connection.invoke('UserTyping', {
                chatRoomId: this.config.chatRoomId,
                userId: this.config.currentUserId,
                userName: this.config.userName,
                isTyping: false
            }).catch(err => console.error('Error sending typing notification:', err));
        }, 1500);
    }
    
    scrollToBottom() {
        if (this.chatMessagesElement) {
            this.chatMessagesElement.scrollTop = this.chatMessagesElement.scrollHeight;
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
            return `${date.toLocaleDateString()} ${date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        }
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    handleDisputeInitiated(data) {
        this.addSystemMessage(`Dispute initiated: ${data.reason}`);
        
        // Show dispute alert
        const alertHtml = `<div class="alert alert-warning">This chat is now in dispute mode. An administrator will review this conversation.</div>`;
        document.getElementById('dispute-container').innerHTML = alertHtml;
    }
    
    handleDisputeResolved(data) {
        this.addSystemMessage(`Dispute resolved: ${data.resolution}`);
        
        // Show resolution alert
        const alertHtml = `<div class="alert alert-success">This dispute has been resolved by an administrator.</div>`;
        document.getElementById('dispute-container').innerHTML = alertHtml;
    }
}

// Initialize the chat when document is ready
document.addEventListener('DOMContentLoaded', function() {
    if (window.chatConfig) {
        try {
            window.chatManager = new ChatManager(window.chatConfig);
        } catch (error) {
            console.error('Error initializing chat manager:', error);
            
            // Show error in chat container
            const chatContainer = document.querySelector('.chat-messages');
            if (chatContainer) {
                chatContainer.innerHTML = `
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle"></i> 
                        Failed to initialize chat. Please refresh the page or contact support.
                    </div>
                `;
            }
        }
    } else {
        console.warn('Chat config not found, skipping chat initialization');
    }
});

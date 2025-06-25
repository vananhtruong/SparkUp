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
            console.log('[INFO] Initializing chat system...');
            
            // Validate configuration
            if (!this.config || !this.config.chatRoomId) {
                throw new Error('Invalid chat configuration: missing chatRoomId');
            }
            
            console.log('[INFO] Chat configuration:', {
                chatRoomId: this.config.chatRoomId,
                currentUserId: this.config.currentUserId,
                userRole: this.config.userRole
            });
            
            // Initialize SignalR connection
            await this.initSignalR();
            console.log('[INFO] SignalR initialized successfully');
            
            // Initialize UI elements
            this.initUI();
            console.log('[INFO] UI initialized successfully');
            
            // Load messages
            await this.loadMessages();
            console.log('[INFO] Messages loaded successfully');
            
            // Start SignalR connection
            await this.startConnection();
            console.log('[INFO] SignalR connection started successfully');
            
            console.log('[INFO] Chat initialization complete!');
            
        } catch (error) {
            console.error('[ERROR] Failed to initialize chat:', error);
            this.showError('Failed to initialize chat. Please refresh the page.');
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
    }    initUI() {
        try {
            console.log('Initializing UI elements');
            
            // Initialize UI elements - updated to match HTML IDs in Room.cshtml
            this.chatMessagesElement = document.getElementById('chat-messages');
            this.messageInputElement = document.getElementById('messageInput');
            this.sendButtonElement = document.getElementById('sendButton');
            
            // Create chat status element if it doesn't exist
            this.chatStatusElement = document.getElementById('chat-status');
            if (!this.chatStatusElement) {
                console.log('Creating chat-status element');
                this.chatStatusElement = document.createElement('div');
                this.chatStatusElement.id = 'chat-status';
                this.chatStatusElement.className = 'chat-status-container';
                this.chatStatusElement.style.display = 'none';
                
                // Insert at the top of chat-messages-container
                const messagesContainer = document.querySelector('.chat-messages-container');
                if (messagesContainer) {
                    messagesContainer.insertBefore(this.chatStatusElement, messagesContainer.firstChild);
                }
            }
            
            // Create typing indicator element if it doesn't exist
            this.typingIndicatorElement = document.getElementById('typingIndicator');
            if (!this.typingIndicatorElement) {
                console.log('Creating typingIndicator element');
                this.typingIndicatorElement = document.createElement('div');
                this.typingIndicatorElement.id = 'typingIndicator';
                this.typingIndicatorElement.className = 'typing-indicator';
                this.typingIndicatorElement.style.display = 'none';
                
                // Insert near the input area
                const inputContainer = document.querySelector('.chat-input-container');
                if (inputContainer) {
                    inputContainer.appendChild(this.typingIndicatorElement);
                }
            }

            console.log('UI elements found:', {
                chatMessagesElement: !!this.chatMessagesElement,
                messageInputElement: !!this.messageInputElement,
                sendButtonElement: !!this.sendButtonElement,
                chatStatusElement: !!this.chatStatusElement,
                typingIndicatorElement: !!this.typingIndicatorElement
            });

            // If critical elements don't exist, show an error
            if (!this.chatMessagesElement) {
                console.error('Critical error: chat-messages element not found');
                // Try to create it as a last resort
                const messagesContainer = document.querySelector('.chat-messages-container');
                if (messagesContainer) {
                    console.log('Creating chat-messages element as fallback');
                    this.chatMessagesElement = document.createElement('div');
                    this.chatMessagesElement.id = 'chat-messages';
                    this.chatMessagesElement.className = 'chat-messages p-3';
                    messagesContainer.appendChild(this.chatMessagesElement);
                } else {
                    return this.showError('Critical UI elements not found. Please refresh the page.');
                }
            }
            
            if (!this.messageInputElement || !this.sendButtonElement) {
                console.error('Required chat UI elements not found');
                return this.showError('Required chat UI elements not found. Please refresh the page.');
            }

            // Remove disabled attribute from input and button
            this.messageInputElement.removeAttribute('disabled');
            this.sendButtonElement.removeAttribute('disabled');

            // Set up event listeners
            this.messageInputElement.addEventListener('keydown', (event) => {
                if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault();
                    this.sendMessage();
                }
            });

            this.messageInputElement.addEventListener('input', () => {
                this.handleTypingEvent();
                
                // Update character count
                const charCountElement = document.getElementById('charCount');
                if (charCountElement) {
                    charCountElement.textContent = this.messageInputElement.value.length;
                }
            });

            this.sendButtonElement.addEventListener('click', () => {
                this.sendMessage();
            });

            // Add a scroll event listener to mark messages as read when scrolled into view
            this.chatMessagesElement.addEventListener('scroll', () => {
                this.checkVisibleMessages();
            });
            
            // Remove loading indicator
            const loadingElement = document.getElementById('loading-messages');
            if (loadingElement) {
                loadingElement.style.display = 'none';
            }
        } catch (error) {
            console.error('Error initializing UI:', error);
            this.showError(`Error initializing UI: ${error.message || 'Unknown error'}`);
        }
    }    async startConnection() {
        try {
            console.log('[INFO] Starting SignalR connection...');
            
            // Check if connection already exists and is connected
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                console.log('[INFO] SignalR already connected');
                return;
            }
            
            await this.connection.start();
            console.log('[INFO] SignalR connection started successfully');
            
            // Wait a bit for connection to stabilize
            await new Promise(resolve => setTimeout(resolve, 200));
            
            // Check connection state before proceeding
            if (this.connection.state === signalR.HubConnectionState.Connected) {
                try {
                    const chatRoomIdStr = this.config.chatRoomId.toString();
                    console.log(`[INFO] Joining chat room: ${chatRoomIdStr}`);
                    await this.connection.invoke('JoinChatRoom', chatRoomIdStr);
                    console.log('[INFO] Successfully joined chat room group');
                    
                    this.showConnectionStatus("Đã kết nối thành công!", "success");
                    setTimeout(() => {
                        const statusElement = document.getElementById('chat-status');
                        if (statusElement) {
                            statusElement.style.display = 'none';
                        }
                    }, 3000);
                    
                } catch (joinError) {
                    console.error('[ERROR] Failed to join chat room:', joinError);
                    this.showConnectionStatus("Không thể tham gia phòng chat", "warning");
                }
            } else {
                console.warn(`[WARN] Connection state after start: ${this.connection.state}`);
                this.showConnectionStatus("Kết nối không ổn định", "warning");
            }
            
        } catch (error) {
            console.error('[ERROR] Failed to start SignalR connection:', error);
            this.showConnectionStatus("Không thể kết nối tới server chat", "error");
            
            // Retry after 3 seconds
            setTimeout(() => {
                console.log('[INFO] Retrying SignalR connection...');
                this.startConnection();
            }, 3000);
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
            
            // Show loading indicator if it exists
            const loadingElement = document.getElementById('loading-messages');
            if (loadingElement) {
                loadingElement.style.display = 'block';
            }
            
            // Create an array of endpoints to try in order
            const endpoints = [
                `/api/chat/messages/${this.config.chatRoomId}`,
                `/Chat/GetMessages/${this.config.chatRoomId}?page=1&pageSize=50`,
                `/Chat/GetMessages?chatRoomId=${this.config.chatRoomId}&page=1&pageSize=50`,
                `/Chat/Api/Messages/${this.config.chatRoomId}?page=1&pageSize=50`
            ];
            
            let response = null;
            let lastError = null;
            let success = false;
            
            // Try each endpoint until one works
            for (const endpoint of endpoints) {
                try {
                    console.log(`Trying endpoint: ${endpoint}`);
                    response = await fetch(endpoint);
                    
                    console.log(`Response from ${endpoint}:`, response.status, response.statusText);
                    
                    if (response.ok) {
                        success = true;
                        console.log(`Successfully got messages from: ${endpoint}`);
                        
                        // Try to peek at response
                        const clone = response.clone();
                        try {
                            const text = await clone.text();
                            console.log(`Response preview (first 200 chars): ${text.substring(0, 200)}`);
                        } catch (err) {
                            console.warn('Could not preview response', err);
                        }
                        
                        break;
                    } else {
                        lastError = `${response.status}: ${response.statusText}`;
                    }
                } catch (error) {
                    lastError = error;
                    console.error(`Failed to fetch messages from ${endpoint}:`, error);
                }
            }
            
            // If no endpoint worked, show error and return
            if (!success || !response || !response.ok) {
                console.error('All message loading attempts failed:', lastError);
                this.showError('Could not load messages. Please refresh the page.');
                
                // Hide loading indicator
                if (loadingElement) {
                    loadingElement.style.display = 'none';
                }
                
                return;
            }
            
            let data;
            try {
                data = await response.json();
                console.log('Received message data:', data);
                
                // Debug data structure
                if (data && data.length > 0) {
                    console.log('Sample message structure:', data[0]);
                } else {
                    console.log('No messages received or empty array');
                }
                
            } catch (jsonError) {
                console.error('Error parsing JSON response:', jsonError);
                this.showError('Could not parse message data. Please refresh the page.');
                
                // Hide loading indicator
                if (loadingElement) {
                    loadingElement.style.display = 'none';
                }
                
                return;
            }
            
            // Handle different response formats and ensure messages is an array
            let messages = [];
            if (data) {
                if (Array.isArray(data)) {
                    messages = data;
                } else if (data.items && Array.isArray(data.items)) {
                    messages = data.items;
                } else if (typeof data === 'object') {
                    // If it's just an object, try to convert it to array
                    console.warn('Received unexpected object format for messages, attempting to convert');
                    try {
                        messages = [data];
                    } catch (err) {
                        console.error('Failed to convert message object to array', err);
                    }
                }
            }
            
            console.log(`Loaded ${messages.length} messages`);
            
            // Sort messages by time if they have sentAt property
            try {
                messages.sort((a, b) => {
                    if (!a.sentAt || !b.sentAt) return 0;
                    return new Date(a.sentAt) - new Date(b.sentAt);
                });
            } catch (sortError) {
                console.warn('Error sorting messages by date:', sortError);
            }
            
            this.messages = messages || [];
            
            // Hide loading indicator
            if (loadingElement) {
                loadingElement.style.display = 'none';
            }
            
            // Display the messages
            this.displayMessages(messages);
            
            // Mark all messages as read
            await this.markAllMessagesAsRead();
            
        } catch (error) {
            console.error('Error loading messages:', error);
            this.showError('Could not load messages. Please refresh the page.');
            
            // Hide loading indicator
            const loadingElement = document.getElementById('loading-messages');
            if (loadingElement) {
                loadingElement.style.display = 'none';
            }
        }
    }    async markAllMessagesAsRead() {
        try {
            // Check if there's a valid chatRoomId
            if (!this.config || !this.config.chatRoomId) {
                console.warn('No valid chatRoomId, skipping markAllMessagesAsRead');
                return;
            }
            
            console.log('Attempting to mark all messages as read for room:', this.config.chatRoomId);
            
            // Create an array of endpoints to try in order
            const endpoints = [
                `/Chat/MarkAllAsRead/${this.config.chatRoomId}`,
                `/api/chat/rooms/${this.config.chatRoomId}/markAllRead`,
                `/api/chat/messages/markAllRead?roomId=${this.config.chatRoomId}`
            ];
            
            let success = false;
            
            // Try each endpoint until one works
            for (const endpoint of endpoints) {
                try {
                    console.log(`Trying to mark all messages as read using: ${endpoint}`);
                    const response = await fetch(endpoint, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    });
                    
                    if (response.ok) {
                        success = true;
                        console.log(`Successfully marked all messages as read using ${endpoint}`);
                        
                        // Update UI - mark all messages as read
                        document.querySelectorAll('.message-status').forEach(el => {
                            el.innerHTML = '<i class="bi bi-check-all"></i>';
                        });
                        
                        break;
                    } else {
                        console.warn(`Failed to mark messages as read with ${endpoint}:`, response.status);
                    }
                } catch (apiError) {
                    console.warn(`API error when marking messages as read with ${endpoint}:`, apiError);
                }
            }
            
            if (!success) {
                console.warn('All attempts to mark messages as read failed');
                // Silent fail - don't show error to user
            }
        } catch (error) {
            console.error('Failed to mark all messages as read:', error);
        }
    }
    
    displayMessages(messages) {
        if (!this.chatMessagesElement) {
            console.error('Chat messages element not found');
            return;
        }
        
        // Clear existing messages
        this.chatMessagesElement.innerHTML = '';
        
        // Ensure messages is an array
        const messageArray = Array.isArray(messages) ? messages : [];
        console.log(`Displaying ${messageArray.length} messages`, messageArray);
        
        if (messageArray.length === 0) {
            const noMessagesEl = document.createElement('div');
            noMessagesEl.className = 'text-center text-muted p-4';
            noMessagesEl.innerHTML = '<div class="mb-2"><i class="bi bi-chat-dots" style="font-size: 2rem;"></i></div><div>Chưa có tin nhắn nào. Hãy bắt đầu cuộc trò chuyện!</div>';
            this.chatMessagesElement.appendChild(noMessagesEl);
            return;
        }
        
        try {
            // Group messages by date
            const messagesByDate = this.groupMessagesByDate(messageArray);
            
            // Add each message group to the DOM
            for (const [date, msgs] of Object.entries(messagesByDate)) {
                // Add date separator
                const dateSeparator = document.createElement('div');
                dateSeparator.className = 'date-separator';
                dateSeparator.innerHTML = `<span>${date}</span>`;
                this.chatMessagesElement.appendChild(dateSeparator);
                
                // Add messages for this date
                msgs.forEach(message => {
                    try {
                        // Debug message data
                        console.log('Processing message:', message);
                        
                        const messageElement = this.createMessageElement(message);
                        this.chatMessagesElement.appendChild(messageElement);
                    } catch (err) {
                        console.error('Error displaying message:', message, err);
                        
                        // Add error message instead
                        const errorMsg = document.createElement('div');
                        errorMsg.className = 'alert alert-danger small';
                        errorMsg.textContent = 'Error displaying this message';
                        this.chatMessagesElement.appendChild(errorMsg);
                    }
                });
            }
        } catch (error) {
            console.error('Error in displayMessages:', error);
            const errorDisplay = document.createElement('div');
            errorDisplay.className = 'alert alert-danger';
            errorDisplay.textContent = 'Error displaying messages. Please refresh the page.';
            this.chatMessagesElement.appendChild(errorDisplay);
        }
        
        // Scroll to the bottom
        this.scrollToBottom();
    }
    
    // Helper method to group messages by date
    groupMessagesByDate(messages) {
        const groups = {};
        
        messages.forEach(msg => {
            if (!msg.sentAt) return;
            
            const date = new Date(msg.sentAt);
            if (isNaN(date.getTime())) return;
            
            const dateString = this.formatDateForGrouping(date);
            
            if (!groups[dateString]) {
                groups[dateString] = [];
            }
            
            groups[dateString].push(msg);
        });
        
        return groups;
    }
    
    // Format date for grouping messages
    formatDateForGrouping(date) {
        const now = new Date();
        const yesterday = new Date();
        yesterday.setDate(yesterday.getDate() - 1);
        
        if (date.toDateString() === now.toDateString()) {
            return 'Hôm nay';
        } else if (date.toDateString() === yesterday.toDateString()) {
            return 'Hôm qua';
        } else {
            return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
        }
    }    sendMessage() {
        const content = this.messageInputElement.value.trim();
        
        console.log('[CHAT] sendMessage called, content:', content);
        
        if (!content) {
            console.log('[CHAT] sendMessage - empty content, aborting');
            return;
        }
        
        if (!this.config || !this.config.chatRoomId) {
            console.error('[CHAT] sendMessage - missing config or chatRoomId');
            this.showError('Chat configuration error. Please refresh the page.');
            return;
        }
        
        const message = {
            chatRoomId: parseInt(this.config.chatRoomId),
            content: content,
            messageType: 0 // Use numeric value for ChatMessageType.Text
        };
        
        console.log('[CHAT] Sending message:', message);
        console.log('[CHAT] Config:', this.config);
        
        // Get CSRF token
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        console.log('[CHAT] CSRF Token found:', !!csrfToken);
        
        // Disable input and button while sending
        this.messageInputElement.disabled = true;
        this.sendButtonElement.disabled = true;
        
        // Clear the input field immediately for better UX
        this.messageInputElement.value = '';
        
        // Reset typing indicator
        this.isTyping = false;
        clearTimeout(this.typingTimer);
        
        // Prepare headers
        const headers = {
            'Content-Type': 'application/json'
        };
        
        if (csrfToken) {
            headers['RequestVerificationToken'] = csrfToken;
        }
        
        console.log('[CHAT] Request headers:', headers);
        
        // Send message via fetch API
        fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(message)
        })
        .then(response => {
            console.log('[CHAT] SendMessage response status:', response.status);
            console.log('[CHAT] SendMessage response headers:', response.headers);
            
            if (!response.ok) {
                return response.text().then(text => {
                    console.error('[CHAT] Error response body:', text);
                    throw new Error(`HTTP ${response.status}: ${text}`);
                });
            }
            return response.json();
        })
        .then(data => {
            console.log('[CHAT] SendMessage success:', data);
            if (data.success) {
                // Reload messages to show the new message
                this.loadMessages();
            } else {
                this.showError('Failed to send message. Please try again.');
                // Restore the message content
                this.messageInputElement.value = content;
            }
        })
        .catch(error => {
            console.error('[CHAT] SendMessage error:', error);
            this.showError('Failed to send message: ' + error.message);
            
            // Restore the message content
            this.messageInputElement.value = content;
        })
        .finally(() => {
            // Re-enable input and button
            this.messageInputElement.disabled = false;
            this.sendButtonElement.disabled = false;
            this.messageInputElement.focus();
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
        try {
            if (!message) {
                console.error('Invalid message data: message is null or undefined');
                const errorDiv = document.createElement('div');
                errorDiv.className = 'alert alert-danger small';
                errorDiv.textContent = 'Error: Invalid message data';
                return errorDiv;
            }
            
            if (!message.id) {
                console.warn('Message missing ID:', message);
                // Try to continue anyway with a generated ID
                message.id = `gen_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            }
            
            console.log('Creating element for message:', message);
            
            const isCurrentUser = message.senderId == this.config.currentUserId || message.isOwnMessage;
            const messageContainer = document.createElement('div');
            messageContainer.className = `message-container ${isCurrentUser ? 'outgoing' : 'incoming'}`;
            messageContainer.dataset.messageId = message.id;
            
            // Create the message content element
            let messageContent = '';
            
            // Check message type (handle as string or enum)
            const messageType = typeof message.messageType === 'string' ? 
                message.messageType : 
                (message.messageType ? message.messageType.toString() : 'Text');
            
            if (messageType === 'System' || messageType === 'DisputeInitiated' || messageType === 'DisputeResolved') {
                // System message
                messageContainer.className = 'message-container system-message';
                messageContent = `
                    <div class="alert alert-info mb-2">
                        <i class="bi bi-info-circle"></i> 
                        ${this.escapeHtml(message.content || '')}
                    </div>
                `;
            } else if (messageType === 'File') {
                // File message
                messageContent = `
                    <div class="message ${isCurrentUser ? 'sent' : 'received'}">
                        <div class="message-sender">${message.senderName || 'Unknown'}</div>
                        <div class="message-file">
                            <i class="bi bi-file-earmark"></i>
                            <a href="/Chat/DownloadFile/${message.id}" target="_blank">${message.content || 'File Attachment'}</a>
                        </div>
                        <div class="message-time">${this.formatTime(message.sentAt)}</div>
                        <div class="message-status">${message.isRead ? '<i class="bi bi-check-all"></i>' : '<i class="bi bi-check"></i>'}</div>
                    </div>
                `;
            } else {
                // Regular text message - Improved styling
                messageContent = `
                    <div class="message ${isCurrentUser ? 'sent' : 'received'}">
                        <div class="message-sender">${message.senderName || 'Unknown'}</div>
                        <div class="message-content">${this.escapeHtml(message.content || '')}</div>
                        <div class="message-meta">
                            <span class="message-time">${this.formatTime(message.sentAt)}</span>
                            <span class="message-status">${message.isRead ? '<i class="bi bi-check-all"></i>' : '<i class="bi bi-check"></i>'}</span>
                        </div>
                    </div>
                `;
            }
            
            messageContainer.innerHTML = messageContent;
            return messageContainer;
        } catch (error) {
            console.error('Error creating message element:', error, message);
            const errorDiv = document.createElement('div');
            errorDiv.className = 'alert alert-danger small';
            errorDiv.textContent = 'Error displaying this message';
            return errorDiv;
        }
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
            console.log(`[CHAT] Marking message ${messageId} as read`);
            
            // Use HTTP API instead of SignalR for better reliability
            const response = await fetch(`/Chat/MarkAsRead/${messageId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            if (response.ok) {
                console.log(`[CHAT] Message ${messageId} marked as read successfully`);
                this.markMessageAsRead(messageId);
            } else {
                console.warn(`[CHAT] Failed to mark message ${messageId} as read: ${response.status}`);
            }
        } catch (error) {
            console.error(`[CHAT] Error marking message ${messageId} as read:`, error);
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
        if (!this.chatStatusElement) {
            // Create error element if it doesn't exist
            const errorContainer = document.createElement('div');
            errorContainer.id = 'chat-status';
            errorContainer.className = 'chat-status-container';
            
            // Try to inject it at the top of chat messages container
            const chatContainer = document.querySelector('.chat-messages-container');
            if (chatContainer) {
                chatContainer.insertBefore(errorContainer, chatContainer.firstChild);
                this.chatStatusElement = errorContainer;
            } else {
                // If chat container not found, add to body
                document.body.appendChild(errorContainer);
                this.chatStatusElement = errorContainer;
            }
        }
        
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
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            console.warn('[CHAT] SignalR not connected, skipping typing notification');
            return;
        }
        
        if (!this.isTyping) {
            this.isTyping = true;
            
            // Notify others that this user is typing
            this.connection.invoke('UserTyping', {
                chatRoomId: this.config.chatRoomId,
                userId: this.config.currentUserId,
                userName: this.config.userName,
                isTyping: true
            }).catch(err => console.warn('Error sending typing notification:', err));
        }
        
        // Clear existing timer
        clearTimeout(this.typingTimer);
        
        // Set a timer to stop typing indicator after 1.5 seconds of inactivity
        this.typingTimer = setTimeout(() => {
            this.isTyping = false;
            
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                // Notify others that this user stopped typing
                this.connection.invoke('UserTyping', {
                    chatRoomId: this.config.chatRoomId,
                    userId: this.config.currentUserId,
                    userName: this.config.userName,
                    isTyping: false
                }).catch(err => console.warn('Error sending typing notification:', err));
            }
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
    console.log('[CHAT] DOM loaded, checking for chat config...');
    
    if (window.chatConfig) {
        console.log('[CHAT] Chat config found:', window.chatConfig);
        try {
            window.chatManager = new ChatManager(window.chatConfig);
        } catch (error) {
            console.error('[CHAT] Error initializing chat manager:', error);
            
            // Show error in chat container
            const chatContainer = document.querySelector('.chat-messages');
            if (chatContainer) {
                chatContainer.innerHTML = `
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle"></i> 
                        Failed to initialize chat. Please refresh the page or contact support.
                        <br><small>Error: ${error.message}</small>
                    </div>
                `;
            }
        }
    } else {
        console.warn('[CHAT] Chat config not found, skipping chat initialization');
        
        // Check if we're on a chat page - if so, show error
        if (window.location.pathname.includes('/Chat/Room/')) {
            const chatContainer = document.querySelector('.chat-messages');
            if (chatContainer) {
                chatContainer.innerHTML = `
                    <div class="alert alert-warning">
                        <i class="bi bi-exclamation-triangle"></i> 
                        Chat configuration not found. Please refresh the page.
                    </div>
                `;
            }
        }
    }
});

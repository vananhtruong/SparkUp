// Real-time Notification System
class NotificationManager {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.retryCount = 0;
        this.maxRetries = 5;
        this.retryDelay = 3000; // 3 seconds
    }

    async initialize() {
        try {
            // Create SignalR connection for notifications
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Start connection
            await this.startConnection();
            
            console.log('[NotificationManager] Initialized successfully');
        } catch (error) {
            console.error('[NotificationManager] Initialization failed:', error);
            this.handleConnectionError();
        }
    }

    setupEventHandlers() {
        // Handle new notifications
        this.connection.on("NewNotification", (notification) => {
            console.log('[NotificationManager] New notification received:', notification);
            this.displayNotification(notification);
            this.playNotificationSound();
        });

        // Handle notification count updates
        this.connection.on("NotificationCount", (count) => {
            console.log('[NotificationManager] Notification count updated:', count);
            this.updateNotificationBadge(count);
        });

        // Handle connection state changes
        this.connection.onreconnecting(() => {
            console.log('[NotificationManager] Reconnecting...');
            this.showConnectionStatus('Đang kết nối lại...', 'warning');
        });

        this.connection.onreconnected(() => {
            console.log('[NotificationManager] Reconnected');
            this.showConnectionStatus('Đã kết nối lại', 'success');
            this.isConnected = true;
            this.retryCount = 0;
            // Request current notification count
            this.requestNotificationCount();
        });

        this.connection.onclose(() => {
            console.log('[NotificationManager] Connection closed');
            this.isConnected = false;
            this.handleConnectionError();
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            this.isConnected = true;
            this.retryCount = 0;
            
            // Request initial notification count
            await this.requestNotificationCount();
            
            console.log('[NotificationManager] Connected to notification hub');
        } catch (error) {
            console.error('[NotificationManager] Connection failed:', error);
            throw error;
        }
    }

    async requestNotificationCount() {
        if (this.isConnected) {
            try {
                await this.connection.invoke("RequestNotificationCount");
            } catch (error) {
                console.error('[NotificationManager] Failed to request notification count:', error);
            }
        }
    }

    displayNotification(notification) {
        // Create toast notification
        const toast = this.createToastElement(notification);
        document.body.appendChild(toast);

        // Show toast with animation
        setTimeout(() => toast.classList.add('show'), 100);

        // Auto hide after 5 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 5000);

        // Handle click to redirect
        toast.addEventListener('click', () => {
            if (notification.redirectUrl) {
                window.location.href = notification.redirectUrl;
            }
            toast.remove();
        });
    }

    createToastElement(notification) {
        const toast = document.createElement('div');
        toast.className = 'notification-toast';
        toast.innerHTML = `
            <div class="notification-icon">
                ${this.getNotificationIcon(notification.type)}
            </div>
            <div class="notification-content">
                <div class="notification-title">${notification.title}</div>
                <div class="notification-message">${notification.content}</div>
                <div class="notification-time">${this.formatTime(notification.createdAt)}</div>
            </div>
            <button class="notification-close" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        `;
        return toast;
    }

    getNotificationIcon(type) {
        const icons = {
            'Chat': '<i class="fas fa-comment text-blue"></i>',
            'Booking': '<i class="fas fa-calendar text-green"></i>',
            'BookingStatus': '<i class="fas fa-clock text-orange"></i>',
            'Payment': '<i class="fas fa-credit-card text-purple"></i>',
            'System': '<i class="fas fa-bell text-gray"></i>'
        };
        return icons[type] || icons['System'];
    }

    formatTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diff = Math.floor((now - date) / 1000); // seconds

        if (diff < 60) return 'Vừa xong';
        if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
        if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
        return date.toLocaleDateString('vi-VN');
    }

    updateNotificationBadge(count) {
        const badges = document.querySelectorAll('.notification-badge');
        badges.forEach(badge => {
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count;
                badge.style.display = 'flex';
            } else {
                badge.style.display = 'none';
            }
        });
    }

    playNotificationSound() {
        // Only play sound if user has interacted with page (browser policy)
        if (document.visibilityState === 'visible') {
            try {
                const audio = new Audio('/sounds/notification.mp3');
                audio.volume = 0.3;
                audio.play().catch(e => console.log('Could not play notification sound:', e));
            } catch (error) {
                console.log('Notification sound not available');
            }
        }
    }

    showConnectionStatus(message, type) {
        // Remove existing status
        const existing = document.querySelector('.connection-status');
        if (existing) existing.remove();

        // Create new status indicator
        const status = document.createElement('div');
        status.className = `connection-status connection-${type}`;
        status.textContent = message;
        document.body.appendChild(status);

        // Auto remove after 3 seconds
        setTimeout(() => {
            if (status.parentNode) status.remove();
        }, 3000);
    }

    handleConnectionError() {
        if (this.retryCount < this.maxRetries) {
            this.retryCount++;
            console.log(`[NotificationManager] Retrying connection (${this.retryCount}/${this.maxRetries})`);
            
            setTimeout(() => {
                this.initialize();
            }, this.retryDelay * this.retryCount);
        } else {
            console.error('[NotificationManager] Max retries reached, giving up');
            this.showConnectionStatus('Mất kết nối thông báo', 'error');
        }
    }    // Public methods for manual operations
    async markNotificationAsRead(notificationId) {
        if (this.isConnected) {
            try {
                await this.connection.invoke("MarkNotificationAsRead", notificationId);
                this.requestNotificationCount(); // Refresh count
            } catch (error) {
                console.error('Failed to mark notification as read:', error);
            }
        }
    }    // Notification menu interaction methods
    markAsRead(id, redirectUrl, action) {
        const token = this.getAntiForgeryToken();
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);
        
        fetch('/Notification/MarkAsRead/' + id, {
            method: 'POST',
            body: formData
        }).then(response => {
            if (response.ok) {
                // Update UI
                const notificationElement = document.querySelector(`.notification-item[data-id="${id}"]`);
                if (notificationElement) {
                    notificationElement.classList.remove('unread');
                    const titleElement = notificationElement.querySelector('.notification-title');
                    if (titleElement) {
                        titleElement.classList.remove('fw-bold');
                    }
                }

                // Update badge count
                this.decrementBadgeCount();

                // Handle redirect based on action
                if (redirectUrl && (action === 'view' || action === 'payment')) {
                    setTimeout(() => {
                        window.location.href = redirectUrl;
                    }, 300);
                }
            }
        }).catch(error => {
            console.error('[NotificationManager] Mark as read failed:', error);
        });
    }    markAllAsRead() {
        const token = this.getAntiForgeryToken();
        const formData = new FormData();
        formData.append('__RequestVerificationToken', token);
        
        fetch('/Notification/MarkAllAsRead', {
            method: 'POST',
            body: formData
        }).then(response => {
            if (response.ok) {
                // Update UI
                document.querySelectorAll('.notification-item.unread').forEach(element => {
                    element.classList.remove('unread');
                    const titleElement = element.querySelector('.notification-title');
                    if (titleElement) {
                        titleElement.classList.remove('fw-bold');
                    }
                });

                // Reset badge count
                this.updateNotificationBadge(0);
            }
        }).catch(error => {
            console.error('[NotificationManager] Mark all as read failed:', error);
        });
    }

    decrementBadgeCount() {
        const badgeElement = document.getElementById('notification-badge');
        const countElement = document.getElementById('notification-count');
        
        if (badgeElement && countElement) {
            const currentCount = parseInt(countElement.textContent) || 0;
            if (currentCount > 1) {
                const newCount = currentCount - 1;
                countElement.textContent = newCount;
                badgeElement.textContent = newCount;
            } else {
                badgeElement.style.display = 'none';
                countElement.style.display = 'none';
            }
        }
    }

    getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }
}

// Global notification manager instance
let notificationManager = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize if user is authenticated
    if (document.querySelector('.notification-toggle') || document.querySelector('.notification-badge')) {
        notificationManager = new NotificationManager();
        notificationManager.initialize();
        
        // Expose to global scope for menu interaction
        window.notificationManager = notificationManager;
    }
});

// Handle page visibility changes
document.addEventListener('visibilitychange', function() {
    if (document.visibilityState === 'visible' && notificationManager && !notificationManager.isConnected) {
        // Try to reconnect when user returns to tab
        notificationManager.initialize();
    }
});

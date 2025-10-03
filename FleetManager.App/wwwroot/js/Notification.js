


        window.notificationUserId = '@_authUser.UserId';

        class NotificationManager {
            constructor() {
            this.connection = null;
        this.notifications = [];
        this.unreadCount = 0;
        this.isConnected = false;
        this.sessionKey = `fm:notifications:${window.notificationUserId}`;
        this.init();
            }

        async init() {
                try {
            await this.setupSignalR();
        await this.loadExistingNotifications();
        this.setupEventHandlers();
                } catch (error) {
            console.error('Failed to initialize notification manager:', error);
                }
            }

        async setupSignalR() {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .withAutomaticReconnect()
                .build();

                this.connection.on("ReceiveNotification", (notification) => {
            this.addNotification(notification, true);
                });

                this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
        this.isConnected = true;
                });

                this.connection.onclose(() => {
            console.log('SignalR disconnected');
        this.isConnected = false;
                });

        await this.connection.start();
        this.isConnected = true;
        console.log('SignalR connected successfully');
            }

        async loadExistingNotifications() {
            try {
            document.getElementById('notificationLoading').style.display = 'none';
        const resp = await fetch('/api/notifications', {credentials: 'same-origin' });
        if (!resp.ok) throw new Error('Could not load');
        const notifications = await resp.json();

                // Keep old unread IDs for comparison (you can remove this block if not needed)
                const previousUnreadIds = new Set(this.notifications.filter(n => !n.isRead).map(n => n.id));

        this.notifications = []; // clear existing
                notifications.forEach(n => this.notifications.push({
            id: n.id || Date.now(),
        title: n.title || 'New Notification',
        message: n.message || '',
        timestamp: n.timestamp || new Date().toISOString(),
        isRead: n.isRead || false,
        type: n.type || 'info',
        userId: n.userId || '',
        data: n.data || { }
                }));

                // ① Calculate the new unread count
                this.unreadCount = this.notifications.filter(n => !n.isRead).length;

                // ② Sort & render
                this.notifications.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp));
        this.updateUI();

        // ─────── NEW: Play sound only if unreadCount increased ───────
        const lastUnread = parseInt(localStorage.getItem('lastUnreadCount') || '0', 10);
                if (this.unreadCount > lastUnread) {
            this.playNotificationSound();
                }
        localStorage.setItem('lastUnreadCount', this.unreadCount);
                // ───────────────────────────────────────────────────────────────

            } catch (error) {
            console.error('Failed to load existing notifications:', error);
        document.getElementById('notificationLoading').style.display = 'none';
            }
        }
        addNotification(notification, isNew = true) {
            this.notifications.splice(0, 0, {
                id: notification.id || Date.now(),
                title: notification.title || 'New Notification',
                message: notification.message || '',
                timestamp: notification.timestamp || new Date().toISOString(),
                isRead: notification.isRead || false,
                type: notification.type || 'info',
                userId: notification.userId || '',
                data: notification.data || {}
            });

        if (!notification.isRead) {
            this.unreadCount++;
                }

                if (this.notifications.length > 50) {
            this.notifications = this.notifications.slice(0, 50);
                }
                this.notifications.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp));
        this.updateUI();

        if (isNew) {
            this.showToastNotification(notification);
        this.playNotificationSound();
                }
            }

        updateUI() {
            this.updateBadge();
        this.updateNotificationList();
        this.updateMarkAllReadButton();
            }

        updateBadge() {
                const badge = document.getElementById('notificationBadge');
                if (this.unreadCount > 0) {
            badge.textContent = this.unreadCount > 99 ? '99+' : this.unreadCount;
        badge.classList.remove('hidden');
                } else {
            badge.classList.add('hidden');
                }
            }

        updateNotificationList() {
                const list = document.getElementById('notificationList');
        list.innerHTML = '';

        if (this.notifications.length === 0) {
            list.innerHTML = '<li class="notification-item text-center text-muted">No notifications yet</li>';
        return;
                }

                this.notifications.forEach(notification => {
                    const li = document.createElement('li');
        li.className = `notification-item ${!notification.isRead ? 'unread' : ''}`;
        li.setAttribute('data-id', notification.id);

        const timeAgo = this.getTimeAgo(notification.timestamp);
        const icon = this.getNotificationIcon(notification.type);

        li.innerHTML = `
        <div class="d-flex">
            <div class="me-3">
                <i class="${icon}" style="color: ${this.getNotificationColor(notification.type)};"></i>
            </div>
            <div class="flex-grow-1">
                <div class="notification-title">${this.escapeHtml(notification.title)}</div>
                <div class="notification-message">${this.escapeHtml(notification.message)}</div>
                <div class="notification-time">${timeAgo}</div>
            </div>
            ${!notification.isRead ? '<div class="ms-2"><i class="fas fa-circle" style="color: #007bff; font-size: 8px;"></i></div>' : ''}
        </div>
        `;
                    li.addEventListener('click', () => this.markAsRead(notification.id));
        list.appendChild(li);
                });
            }

        updateMarkAllReadButton() {
                const button = document.getElementById('markAllRead');
                button.style.display = this.unreadCount > 0 ? 'inline-block' : 'none';
            }

        getNotificationIcon(type) {
                const t = String(type).toLowerCase();
        const icons = {
            'info': 'fas fa-info-circle',
        'success': 'fas fa-check-circle',
        'warning': 'fas fa-exclamation-triangle',
        'error': 'fas fa-times-circle',
        'vehicle': 'fas fa-car',
        'maintenance': 'fas fa-wrench',
        'driver': 'fas fa-user',
        'alert': 'fas fa-bell'
                };
        return icons[t] || icons['alert'];
            }

        getNotificationColor(type) {
                const t = String(type).toLowerCase();
        const colors = {
            'info': '#17a2b8',
        'success': '#28a745',
        'warning': '#ffc107',
        'error': '#dc3545',
        'vehicle': '#6f42c1',
        'maintenance': '#fd7e14',
        'driver': '#20c997',
        'alert': '#007bff'
                };
        return colors[t] || colors['alert'];
            }

        getTimeAgo(timestamp) {
                try {
                    if (typeof moment !== 'undefined') {
            moment.locale('en'); // set your preferred locale
        return moment.utc(timestamp).local().fromNow(); // ← Correct usage
                    }
                } catch (e) {
            console.warn('Moment.js not available or failed:', e);
                }

        // fallback
        const now = new Date();
        const time = new Date(timestamp);
        const diff = Math.floor((now - time) / 1000);

        if (diff < 60) return 'Just now';
        if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
        if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
        if (diff < 604800) return `${Math.floor(diff / 86400)}d ago`;
        return time.toLocaleDateString();
            }

        escapeHtml(str) {
                return str
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
                    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
            }

            async markAsRead(notificationId) {
    const notif = this.notifications.find(n => n.id == notificationId);
    if (notif && !notif.isRead) {
        notif.isRead = true;
        this.unreadCount = Math.max(0, this.unreadCount - 1);
        this.updateUI();

        try {
            await fetch('/api/notifications/mark-read', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ notificationId })
            });
        } catch (err) {
            console.error('Failed to mark notification as read:', err);
        }
    }
}

            async markAllAsRead() {
    this.notifications.forEach(n => n.isRead = true);
    this.unreadCount = 0;
    this.updateUI();

    try {
        await fetch('/api/notifications/mark-all-read', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
    } catch (err) {
        console.error('Failed to mark all notifications as read:', err);
    }
}

showToastNotification(notification) {
    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    toast.style.cssText = `
                    position: fixed;
                    top: 80px;
                    right: 20px;
                    background: white;
                    border: 1px solid #ddd;
                    border-radius: 8px;
                    padding: 15px;
                    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                    z-index: 9999;
                    max-width: 350px;
                    animation: slideIn 0.3s ease-out;
                `;
    toast.innerHTML = `
                    <div class="d-flex align-items-start">
                        <i class="${this.getNotificationIcon(notification.type)}" style="color: ${this.getNotificationColor(notification.type)}; margin-right: 10px; margin-top: 2px;"></i>
                        <div class="flex-grow-1">
                            <div style="font-weight: 600; margin-bottom: 4px;">${this.escapeHtml(notification.title)}</div>
                            <div style="font-size: 14px; color: #666;">${this.escapeHtml(notification.message)}</div>
                        </div>
                        <button onclick="this.closest('.toast-notification').remove()" style="border:none;background:none;color:#999;font-size:18px;line-height:1;margin-left:10px;">&times;</button>
                    </div>
                `;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
}

playNotificationSound() {
    try {
        const audio = document.getElementById('notification-sound');
        if (audio) {
            audio.currentTime = 0;
            audio.play().catch(() => {
                console.warn('Notification sound blocked by browser autoplay policy');
            });
        }
    } catch (e) {
        console.warn('Could not play notification sound:', e);
    }
}

setupEventHandlers() {
    document.getElementById('markAllRead').addEventListener('click', () => this.markAllAsRead());
    document.getElementById('notificationDropdown').addEventListener('click', () => this.updateUI());
}
        }

function viewAllNotifications() {
    window.location.href = '/notifications';
}

const style = document.createElement('style');
style.textContent = `
        @@keyframes slideIn {
            from { transform: translateX(100%); opacity: 0; }
            to   { transform: translateX(0);    opacity: 1; }
        }
        `;
document.head.appendChild(style);

document.addEventListener('DOMContentLoaded', () => {
    window.notificationManager = new NotificationManager();
});

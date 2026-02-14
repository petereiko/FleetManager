// notifications-page.js - Full notifications page functionality

class NotificationsPageManager {
    constructor() {
        this.notifications = [];
        this.filteredNotifications = [];
        this.currentPage = 1;
        this.itemsPerPage = 10;
        this.filterStatus = 'all';
        this.filterType = 'all';
        this.connection = null;
        this.pendingDeleteId = null;
        this.init();
    }

    async init() {
        try {
            await this.setupSignalR();
            await this.loadNotifications();
            this.setupEventHandlers();
            this.setupDeleteModalIntegration();
        } catch (error) {
            console.error('Failed to initialize notifications page:', error);
            this.showError('Failed to load notifications');
        }
    }

    async setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        this.connection.on("ReceiveNotification", (notification) => {
            this.addNewNotification(notification);
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
        });

        await this.connection.start();
        console.log('SignalR connected successfully');
    }

    setupDeleteModalIntegration() {
        const deleteForm = document.getElementById('deleteForm');
        if (deleteForm) {
            deleteForm.addEventListener('submit', async (e) => {
                if (this.pendingDeleteId) {
                    e.preventDefault();
                    const submitBtn = document.getElementById('deleteSubmitBtn');
                    
                    if (submitBtn) {
                        submitBtn.disabled = true;
                        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Deleting...';
                    }

                    await this.deleteNotification(this.pendingDeleteId);
                    this.pendingDeleteId = null;

                    const deleteModal = document.getElementById('deleteModal');
                    if (deleteModal) {
                        const modalInstance = bootstrap.Modal.getInstance(deleteModal);
                        if (modalInstance) {
                            modalInstance.hide();
                        }
                    }

                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = '<i class="fas fa-trash me-1"></i>Delete <span id="deleteButtonText">Record</span>';
                    }
                }
            });
        }
    }

    async loadNotifications() {
        try {
            document.getElementById('loadingState').style.display = 'block';
            document.getElementById('emptyState').style.display = 'none';
            document.getElementById('notificationsList').style.display = 'none';

            const response = await fetch('/api/notifications/get-recent', {
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error('Failed to load notifications');
            }

            const result = await response.json();
            this.notifications = result.data || [];

            // Sort by timestamp (newest first)
            this.notifications.sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp));

            document.getElementById('loadingState').style.display = 'none';

            if (this.notifications.length === 0) {
                document.getElementById('emptyState').style.display = 'block';
            } else {
                this.applyFilters();
                this.updateStats();
                this.renderNotifications();
            }
        } catch (error) {
            console.error('Error loading notifications:', error);
            document.getElementById('loadingState').style.display = 'none';
            this.showError('Failed to load notifications');
        }
    }

    addNewNotification(notification) {
        this.notifications.unshift({
            id: notification.id || Date.now(),
            title: notification.title || 'New Notification',
            message: notification.message || '',
            timestamp: notification.timestamp || new Date().toISOString(),
            isRead: notification.isRead || false,
            type: notification.type || 'info',
            userId: notification.userId || '',
            data: notification.data || {}
        });

        this.applyFilters();
        this.updateStats();
        this.renderNotifications();
        this.showToast('New notification received', 'success');
    }

    applyFilters() {
        this.filteredNotifications = this.notifications.filter(n => {
            const statusMatch = this.filterStatus === 'all' || 
                               (this.filterStatus === 'unread' && !n.isRead) ||
                               (this.filterStatus === 'read' && n.isRead);
            
            const typeMatch = this.filterType === 'all' || 
                             n.type.toLowerCase() === this.filterType.toLowerCase();

            return statusMatch && typeMatch;
        });

        this.currentPage = 1; // Reset to first page when filters change
    }

    updateStats() {
        const total = this.notifications.length;
        const unread = this.notifications.filter(n => !n.isRead).length;
        const read = total - unread;

        document.getElementById('totalCount').textContent = total;
        document.getElementById('unreadCount').textContent = unread;
        document.getElementById('readCount').textContent = read;

        const markAllBtn = document.getElementById('markAllReadBtn');
        markAllBtn.style.display = unread > 0 ? 'block' : 'none';
    }

    renderNotifications() {
        const container = document.getElementById('notificationsList');
        
        if (this.filteredNotifications.length === 0) {
            container.style.display = 'none';
            document.getElementById('emptyState').style.display = 'block';
            document.getElementById('paginationContainer').style.display = 'none';
            return;
        }

        document.getElementById('emptyState').style.display = 'none';
        container.style.display = 'block';

        // Calculate pagination
        const totalPages = Math.ceil(this.filteredNotifications.length / this.itemsPerPage);
        const startIndex = (this.currentPage - 1) * this.itemsPerPage;
        const endIndex = startIndex + this.itemsPerPage;
        const pageNotifications = this.filteredNotifications.slice(startIndex, endIndex);

        // Render notifications
        container.innerHTML = pageNotifications.map(n => this.createNotificationCard(n)).join('');

        // Attach event listeners
        this.attachEventListeners();

        // Render pagination
        this.renderPagination(totalPages);

        // Update pagination info
        const paginationInfo = document.getElementById('paginationInfo');
        paginationInfo.textContent = `Showing ${startIndex + 1} to ${Math.min(endIndex, this.filteredNotifications.length)} of ${this.filteredNotifications.length} notifications`;
    }

    createNotificationCard(notification) {
        const timeAgo = this.getTimeAgo(notification.timestamp);
        const icon = this.getNotificationIcon(notification.type);
        const iconClass = `icon-${notification.type.toLowerCase()}`;
        const typeClass = `type-${notification.type.toLowerCase()}`;

        return `
            <div class="notification-card ${notification.isRead ? 'read' : 'unread'}" data-id="${notification.id}">
                ${!notification.isRead ? '<div class="unread-indicator"></div>' : ''}
                <div class="notification-header">
                    <div class="notification-icon ${iconClass}">
                        <i class="${icon}" style="color: white;"></i>
                    </div>
                    <div class="notification-content">
                        <div class="notification-title">${this.escapeHtml(notification.title)}</div>
                        <div class="notification-message">${this.escapeHtml(notification.message)}</div>
                        <div class="notification-meta">
                            <span class="notification-time">
                                <i class="far fa-clock"></i>
                                ${timeAgo}
                            </span>
                            <span class="notification-type-badge ${typeClass}">
                                ${notification.type}
                            </span>
                        </div>
                    </div>
                </div>
                <div class="notification-actions">
                    ${!notification.isRead ? `
                        <button class="btn btn-sm btn-primary mark-read-btn" data-id="${notification.id}">
                            <i class="fas fa-check me-1"></i>Mark as Read
                        </button>
                    ` : ''}
                    <button class="btn btn-sm btn-danger delete-notification-btn" 
                            data-notification-id="${notification.id}"
                            data-notification-title="${this.escapeHtml(notification.title)}">
                        <i class="fas fa-trash-alt me-1"></i>Delete
                    </button>
                </div>
            </div>
        `;
    }

    renderPagination(totalPages) {
        const paginationContainer = document.getElementById('paginationContainer');
        const pagination = document.getElementById('pagination');

        if (totalPages <= 1) {
            paginationContainer.style.display = 'none';
            return;
        }

        paginationContainer.style.display = 'block';

        let paginationHTML = '';

        // Previous button
        paginationHTML += `
            <li class="page-item ${this.currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${this.currentPage - 1}">
                    <i class="fas fa-chevron-left"></i>
                </a>
            </li>
        `;

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            if (
                i === 1 || 
                i === totalPages || 
                (i >= this.currentPage - 1 && i <= this.currentPage + 1)
            ) {
                paginationHTML += `
                    <li class="page-item ${i === this.currentPage ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${i}">${i}</a>
                    </li>
                `;
            } else if (i === this.currentPage - 2 || i === this.currentPage + 2) {
                paginationHTML += `
                    <li class="page-item disabled">
                        <span class="page-link">...</span>
                    </li>
                `;
            }
        }

        // Next button
        paginationHTML += `
            <li class="page-item ${this.currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${this.currentPage + 1}">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;

        pagination.innerHTML = paginationHTML;

        // Attach pagination event listeners
        pagination.querySelectorAll('.page-link').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(link.dataset.page);
                if (page && page !== this.currentPage && page >= 1 && page <= totalPages) {
                    this.currentPage = page;
                    this.renderNotifications();
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        });
    }

    attachEventListeners() {
        // Mark as read buttons
        document.querySelectorAll('.mark-read-btn').forEach(btn => {
            btn.addEventListener('click', async () => {
                const id = parseInt(btn.dataset.id);
                await this.markAsRead(id);
            });
        });

        // Delete buttons
        document.querySelectorAll('.delete-notification-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = btn.dataset.notificationId;
                const title = btn.dataset.notificationTitle;
                this.showDeleteModal(id, title);
            });
        });
    }

    async markAsRead(notificationId) {
        try {
            const response = await fetch('/api/notifications/mark-read', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify({ notificationId })
            });

            if (!response.ok) {
                throw new Error('Failed to mark as read');
            }

            // Update local state
            const notification = this.notifications.find(n => n.id === notificationId);
            if (notification) {
                notification.isRead = true;
            }

            this.applyFilters();
            this.updateStats();
            this.renderNotifications();
            this.showToast('Notification marked as read', 'success');

        } catch (error) {
            console.error('Error marking as read:', error);
            this.showToast('Failed to mark as read', 'error');
        }
    }

    async markAllAsRead() {
        try {
            const response = await fetch('/api/notifications/mark-all-read', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error('Failed to mark all as read');
            }

            // Update local state
            this.notifications.forEach(n => n.isRead = true);

            this.applyFilters();
            this.updateStats();
            this.renderNotifications();
            this.showToast('All notifications marked as read', 'success');

        } catch (error) {
            console.error('Error marking all as read:', error);
            this.showToast('Failed to mark all as read', 'error');
        }
    }

    showDeleteModal(notificationId, notificationTitle) {
        this.pendingDeleteId = notificationId;

        const recordNameEl = document.getElementById('deleteRecordName');
        const itemTypeEl = document.getElementById('deleteItemType');
        const buttonTextEl = document.getElementById('deleteButtonText');

        if (recordNameEl) {
            recordNameEl.textContent = notificationTitle || 'this notification';
        }
        if (itemTypeEl) {
            itemTypeEl.textContent = 'notification';
        }
        if (buttonTextEl) {
            buttonTextEl.textContent = 'Notification';
        }

        const deleteModal = document.getElementById('deleteModal');
        if (deleteModal) {
            const modalInstance = bootstrap.Modal.getOrCreateInstance(deleteModal);
            modalInstance.show();
        }
    }

    async deleteNotification(notificationId) {
        try {
            const response = await fetch(`/api/notifications/${notificationId}`, {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error('Failed to delete notification');
            }

            // Remove from local state
            this.notifications = this.notifications.filter(n => n.id != notificationId);

            this.applyFilters();
            this.updateStats();
            this.renderNotifications();
            this.showToast('Notification deleted successfully', 'success');

        } catch (error) {
            console.error('Error deleting notification:', error);
            this.showToast('Failed to delete notification', 'error');
        }
    }

    setupEventHandlers() {
        // Filter by status
        document.getElementById('filterStatus').addEventListener('change', (e) => {
            this.filterStatus = e.target.value;
            this.applyFilters();
            this.renderNotifications();
        });

        // Filter by type
        document.getElementById('filterType').addEventListener('change', (e) => {
            this.filterType = e.target.value;
            this.applyFilters();
            this.renderNotifications();
        });

        // Mark all as read
        document.getElementById('markAllReadBtn').addEventListener('click', () => {
            this.markAllAsRead();
        });
    }

    getNotificationIcon(type) {
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
        return icons[type.toLowerCase()] || icons['alert'];
    }

    getTimeAgo(timestamp) {
        try {
            if (typeof moment !== 'undefined') {
                return moment.utc(timestamp).local().fromNow();
            }
        } catch (e) {
            console.warn('Moment.js not available:', e);
        }

        const now = new Date();
        const time = new Date(timestamp);
        const diff = Math.floor((now - time) / 1000);

        if (diff < 60) return 'Just now';
        if (diff < 3600) return `${Math.floor(diff / 60)} minutes ago`;
        if (diff < 86400) return `${Math.floor(diff / 3600)} hours ago`;
        if (diff < 604800) return `${Math.floor(diff / 86400)} days ago`;
        return time.toLocaleDateString();
    }

    escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    showToast(message, type = 'info') {
        const colors = {
            'success': 'linear-gradient(135deg, #28a745, #20c997)',
            'error': 'linear-gradient(135deg, #dc3545, #c82333)',
            'info': 'linear-gradient(135deg, #17a2b8, #138496)'
        };

        const icons = {
            'success': 'fas fa-check-circle',
            'error': 'fas fa-exclamation-circle',
            'info': 'fas fa-info-circle'
        };

        const toast = document.createElement('div');
        toast.style.cssText = `
            position: fixed;
            top: 80px;
            right: 20px;
            background: ${colors[type] || colors['info']};
            color: white;
            border-radius: 12px;
            padding: 14px 20px;
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3);
            z-index: 9999;
            animation: slideIn 0.3s ease-out;
        `;
        toast.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="${icons[type] || icons['info']} me-2" style="font-size: 18px;"></i>
                <span style="font-weight: 500;">${this.escapeHtml(message)}</span>
            </div>
        `;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    }

    showError(message) {
        const container = document.getElementById('notificationsList');
        container.style.display = 'block';
        container.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                ${this.escapeHtml(message)}
            </div>
        `;
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    window.notificationsPageManager = new NotificationsPageManager();
});
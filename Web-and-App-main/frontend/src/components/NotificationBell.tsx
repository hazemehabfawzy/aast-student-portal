import React, { useEffect, useState } from 'react';
import apiClient from '../api/apiClient';

interface NotificationItem {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

export const NotificationBell: React.FC = () => {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [open, setOpen] = useState(false);

  const fetchNotifications = async () => {
    try {
      const res = await apiClient.get<NotificationItem[]>('/notifications');
      setNotifications(res.data || []);
    } catch {
      // Non-student roles or network errors — keep existing state
    }
  };

  useEffect(() => {
    fetchNotifications();
    const interval = setInterval(fetchNotifications, 60000);
    return () => clearInterval(interval);
  }, []);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  const markRead = async (id: string) => {
    try {
      await apiClient.put(`/notifications/${id}/read`);
      fetchNotifications();
    } catch {
      // ignore
    }
  };

  const markAllRead = async () => {
    const unread = notifications.filter((n) => !n.isRead);
    for (const n of unread) {
      await markRead(n.id);
    }
  };

  const getIcon = (type: string) => {
    switch (type) {
      case 'result': return '📊';
      case 'attendance_warning': return '⚠️';
      case 'chat': return '💬';
      default: return '🔔';
    }
  };

  return (
    <div style={{ position: 'relative' }}>
      <button
        type="button"
        onClick={() => setOpen(!open)}
        style={{
          background: 'none',
          border: 'none',
          cursor: 'pointer',
          fontSize: '22px',
          position: 'relative',
          padding: '4px',
        }}
        aria-label="Notifications"
      >
        🔔
        {unreadCount > 0 && (
          <span
            style={{
              position: 'absolute',
              top: '-4px',
              right: '-4px',
              background: '#EF4444',
              color: '#fff',
              borderRadius: '50%',
              width: '18px',
              height: '18px',
              fontSize: '11px',
              fontWeight: 'bold',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div
          style={{
            position: 'absolute',
            right: 0,
            top: '36px',
            width: '320px',
            background: '#1A2F45',
            border: '1px solid #2A4A6A',
            borderRadius: '12px',
            boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
            zIndex: 1000,
            maxHeight: '400px',
            overflowY: 'auto',
          }}
        >
          <div
            style={{
              padding: '12px 16px',
              borderBottom: '1px solid #2A4A6A',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <span style={{ color: '#fff', fontWeight: 'bold' }}>Notifications</span>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={markAllRead}
                style={{
                  background: 'none',
                  border: 'none',
                  color: '#4A90E2',
                  cursor: 'pointer',
                  fontSize: '12px',
                }}
              >
                Mark all read
              </button>
            )}
          </div>

          {notifications.length === 0 ? (
            <div style={{ padding: '24px', textAlign: 'center', color: '#8AAAC8' }}>
              No notifications
            </div>
          ) : (
            notifications.slice(0, 5).map((n) => (
              <div
                key={n.id}
                onClick={() => markRead(n.id)}
                style={{
                  padding: '12px 16px',
                  borderBottom: '1px solid #1A2F45',
                  background: n.isRead ? 'transparent' : '#0D1B2A33',
                  cursor: 'pointer',
                  borderLeft: n.isRead ? 'none' : '3px solid #4A90E2',
                  display: 'flex',
                  gap: '12px',
                  alignItems: 'flex-start',
                }}
              >
                <div style={{ fontSize: '1.2rem', marginTop: '2px' }}>
                  {getIcon(n.type)}
                </div>
                <div style={{ flex: 1 }}>
                  <div
                    style={{
                      color: '#fff',
                      fontSize: '13px',
                      fontWeight: n.isRead ? 'normal' : 'bold',
                    }}
                  >
                    {n.title}
                  </div>
                  <div style={{ color: '#8AAAC8', fontSize: '12px', marginTop: '2px' }}>
                    {n.body}
                  </div>
                  <div style={{ color: '#4A6A8A', fontSize: '11px', marginTop: '4px' }}>
                    {new Date(n.createdAt).toLocaleString()}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface NotificationItem {
  id: number;
  type: string; // result, attendance_warning, general
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

export const StudentNotifications: React.FC = () => {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchNotifications = () => {
    apiClient.get<NotificationItem[]>('/students/me/notifications')
      .then((res) => {
        // Sort newest first
        const sorted = res.data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
        setNotifications(sorted);
      })
      .catch((err) => {
        console.error('Failed to fetch notifications', err);
      })
      .finally(() => {
        setLoading(false);
      });
  };

  useEffect(() => {
    fetchNotifications();
  }, []);

  const handleMarkAsRead = (id: number) => {
    apiClient.put(`/notifications/${id}/read`)
      .then(() => {
        setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
      })
      .catch((err) => {
        console.error('Failed to mark notification as read', err);
      });
  };

  const getIcon = (type: string) => {
    switch (type) {
      case 'result': return '📊';
      case 'attendance_warning': return '⚠️';
      default: return '🔔';
    }
  };

  if (loading) {
    return <div className="brand-subtitle">Loading notifications...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>My Notifications</h1>
        <p style={{ color: 'var(--text-muted)' }}>Important warnings and published results announcements.</p>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
        {notifications.length === 0 ? (
          <div className="glass-panel" style={{ padding: '40px', textAlign: 'center', color: 'var(--text-muted)' }}>
            No notifications found.
          </div>
        ) : (
          notifications.map((n) => (
            <div
              key={n.id}
              className="glass-panel"
              style={{
                padding: '20px',
                display: 'flex',
                gap: '16px',
                alignItems: 'flex-start',
                opacity: n.isRead ? 0.7 : 1,
                borderLeft: n.isRead ? '1px solid var(--border-color)' : '4px solid var(--primary)',
                transition: 'var(--transition)'
              }}
            >
              <div style={{ fontSize: '1.5rem', marginTop: '2px' }}>
                {getIcon(n.type)}
              </div>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '8px' }}>
                  <h4 style={{ margin: 0, color: n.isRead ? 'var(--text-main)' : 'var(--accent)' }}>{n.title}</h4>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    {new Date(n.createdAt).toLocaleDateString()} {new Date(n.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </span>
                </div>
                <p style={{ marginTop: '8px', color: 'var(--text-main)', fontSize: '0.95rem' }}>{n.body}</p>
                {!n.isRead && (
                  <button
                    className="glass-btn"
                    style={{ marginTop: '12px', padding: '6px 12px', fontSize: '0.8rem' }}
                    onClick={() => handleMarkAsRead(n.id)}
                  >
                    Mark as Read
                  </button>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

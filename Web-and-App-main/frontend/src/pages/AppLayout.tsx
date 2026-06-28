import React from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export const AppLayout: React.FC = () => {
  const { role, fullName, logout, email } = useAuth();

  const getNavLinks = () => {
    switch (role) {
      case 'student':
        return [
          { to: '/student/profile', label: 'Profile', icon: '👤' },
          { to: '/student/results', label: 'Academic Results', icon: '📊' },
          { to: '/student/schedule', label: 'Weekly Schedule', icon: '📅' },
          // { to: '/student/assignments', label: 'Assignments', icon: '📝' }, // disabled until primary API has assignments
          { to: '/student/register', label: 'Register Courses', icon: '📝' },
          { to: '/student/notifications', label: 'Notifications', icon: '🔔' },
        ];
      case 'instructor':
        return [
          { to: '/instructor/sections', label: 'My Sections', icon: '📚' },
          { to: '/instructor/attendance', label: 'Take Attendance', icon: '⏱️' },
          { to: '/instructor/grading', label: 'Grading Portal', icon: '✍️' },
          // { to: '/instructor/assignments', label: 'Assignments', icon: '📝' }, // disabled until primary API has assignments
        ];
      case 'admin':
        return [
          { to: '/admin/students', label: 'Students Directory', icon: '👥' },
          { to: '/admin/instructors', label: 'Instructors', icon: '🧑‍🏫' },
          { to: '/admin/courses', label: 'Courses Catalog', icon: '📖' },
          { to: '/admin/sections', label: 'Sections Management', icon: '🗓️' },
          { to: '/admin/policies', label: 'Grading Policies', icon: '⚖️' },
          { to: '/admin/import', label: 'Bulk Student Import', icon: '📥' },
          { to: '/admin/reports', label: 'System Reports', icon: '📁' },
        ];
      default:
        return [];
    }
  };

  const navLinks = getNavLinks();

  return (
    <div className="app-container">
      <aside className="sidebar glass-panel">
        <div style={{ marginBottom: '32px' }}>
          <div className="brand-title" style={{ fontSize: '1.6rem', marginBottom: '2px', letterSpacing: '-0.5px' }}>
            AAST PORTAL
          </div>
          <div className="brand-subtitle" style={{ fontSize: '0.7rem', margin: 0, textTransform: 'uppercase' }}>
            {role} Workspace
          </div>
        </div>

        <nav style={{ display: 'flex', flexDirection: 'column', gap: '8px', flex: 1, overflowY: 'auto' }}>
          {navLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) => `glass-btn ${isActive ? 'primary' : ''}`}
              style={{
                width: '100%',
                justifyContent: 'flex-start',
                padding: '12px 16px',
                border: 'none',
                background: 'rgba(255,255,255,0.02)',
              }}
            >
              <span style={{ marginRight: '8px' }}>{link.icon}</span>
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div style={{ marginTop: 'auto', paddingTop: '20px', borderTop: '1px solid var(--border-color)' }}>
          <div style={{ marginBottom: '16px', padding: '0 8px' }}>
            <div style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-main)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {fullName}
            </div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {email}
            </div>
          </div>
          <button
            className="glass-btn"
            style={{
              width: '100%',
              justifyContent: 'center',
              borderColor: 'var(--error)',
              color: 'var(--error)',
              background: 'rgba(239, 68, 68, 0.05)',
            }}
            onClick={logout}
          >
            🚪 Log Out
          </button>
        </div>
      </aside>

      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
};

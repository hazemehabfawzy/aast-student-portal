import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export const Dashboard: React.FC = () => {
  const { role, isAuthenticated, loading } = useAuth();

  if (loading) {
    return (
      <div className="auth-page">
        <div className="auth-card glass-panel">
          <div className="brand-title">AAST PORTAL</div>
          <div className="brand-subtitle">Redirecting you...</div>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  switch (role) {
    case 'student':
      return <Navigate to="/student/profile" replace />;
    case 'instructor':
      return <Navigate to="/instructor/sections" replace />;
    case 'admin':
      return <Navigate to="/admin/students" replace />;
    default:
      return (
        <div className="auth-page">
          <div className="auth-card glass-panel">
            <div className="brand-title">AAST PORTAL</div>
            <div className="brand-subtitle" style={{ color: 'var(--error)' }}>Role Configuration Error</div>
            <p style={{ color: 'var(--text-muted)' }}>
              No valid role claims found in your authentication token. Please contact the administrator.
            </p>
          </div>
        </div>
      );
  }
};

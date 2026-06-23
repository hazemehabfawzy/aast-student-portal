import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export const Login: React.FC = () => {
  const { isAuthenticated, login, loading } = useAuth();

  if (loading) {
    return (
      <div className="auth-page">
        <div className="auth-card glass-panel">
          <div className="brand-title">AAST PORTAL</div>
          <div className="brand-subtitle">Initializing Secure Authentication...</div>
          <div style={{ color: 'var(--text-muted)' }}>Please wait a moment.</div>
        </div>
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className="auth-page">
      <div className="auth-card glass-panel" style={{ padding: '48px 32px' }}>
        <div className="brand-title" style={{ fontSize: '2.5rem', marginBottom: '8px' }}>AAST PORTAL</div>
        <div className="brand-subtitle">Computer Engineering Department</div>
        <p style={{ marginBottom: '32px', color: 'var(--text-muted)', lineHeight: '1.6' }}>
          Welcome to the connected Student Portal system. Please log in using your academic credentials.
        </p>
        <button 
          className="glass-btn primary" 
          style={{ width: '100%', justifyContent: 'center', fontSize: '1rem', padding: '14px 20px' }} 
          onClick={login}
        >
          🔑 Log In with Keycloak
        </button>
      </div>
    </div>
  );
};

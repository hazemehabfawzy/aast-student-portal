import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';

interface ProtectedRouteProps {
  allowedRoles?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowedRoles }) => {
  const { isAuthenticated, role, loading } = useAuth();

  if (loading) {
    return (
      <div className="auth-page">
        <div className="auth-card glass-panel">
          <div className="brand-title">AAST PORTAL</div>
          <div className="brand-subtitle">Checking access rights...</div>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    // If not authenticated, we can either redirect to /login or trigger the keycloak login directly.
    // The requirement says: /login -> Keycloak login redirect.
    // So we redirect the user to /login route.
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    // Authenticated but unauthorized role. Redirect to dashboard so they get routed correctly or see error.
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
};

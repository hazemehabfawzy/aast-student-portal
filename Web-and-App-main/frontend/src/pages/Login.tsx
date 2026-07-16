import React, { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export const Login: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login, isAuthenticated, loading: authLoading } = useAuth();
  const navigate = useNavigate();

  if (authLoading) {
    return (
      <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#0D1B2A' }}>
        <p style={{ color: '#8AAAC8' }}>Loading...</p>
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(username, password);
      navigate('/dashboard');
    } catch {
      setError('Invalid username or password');
    }
    setLoading(false);
  };

  return (
    <div style={{
      minHeight: '100vh', display: 'flex',
      alignItems: 'center', justifyContent: 'center',
      background: '#0D1B2A',
    }}>
      <div style={{
        background: '#1A2F45', borderRadius: '16px',
        padding: '48px', width: '100%', maxWidth: '400px',
        border: '1px solid #2A4A6A',
      }}>
        <div style={{ textAlign: 'center', marginBottom: '32px' }}>
          <div style={{ fontSize: '48px', marginBottom: '12px' }}>🎓</div>
          <h1 style={{ color: '#fff', fontSize: '24px', fontWeight: 'bold', margin: 0 }}>
            AAST Student Portal
          </h1>
          <p style={{ color: '#8AAAC8', fontSize: '14px', marginTop: '8px' }}>
            Computer Engineering Department
          </p>
        </div>

        <form onSubmit={handleLogin}>
          <div style={{ marginBottom: '16px' }}>
            <label style={{ color: '#8AAAC8', fontSize: '13px', display: 'block', marginBottom: '6px' }}>
              Username
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter your username"
              required
              style={{
                width: '100%', padding: '12px 14px',
                background: '#0D1B2A', border: '1px solid #2A4A6A',
                borderRadius: '8px', color: '#fff',
                fontSize: '14px', boxSizing: 'border-box',
                outline: 'none',
              }}
            />
          </div>

          <div style={{ marginBottom: '24px' }}>
            <label style={{ color: '#8AAAC8', fontSize: '13px', display: 'block', marginBottom: '6px' }}>
              Password
            </label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              required
              style={{
                width: '100%', padding: '12px 14px',
                background: '#0D1B2A', border: '1px solid #2A4A6A',
                borderRadius: '8px', color: '#fff',
                fontSize: '14px', boxSizing: 'border-box',
                outline: 'none',
              }}
            />
          </div>

          {error && (
            <div style={{
              background: '#EF444422', border: '1px solid #EF4444',
              borderRadius: '8px', padding: '10px 14px',
              color: '#EF4444', fontSize: '13px',
              marginBottom: '16px',
            }}>
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            style={{
              width: '100%', padding: '14px',
              background: loading ? '#2A4A6A' : '#4A90E2',
              border: 'none', borderRadius: '8px',
              color: '#fff', fontSize: '15px',
              fontWeight: 'bold', cursor: loading ? 'not-allowed' : 'pointer',
            }}
          >
            {loading ? 'Signing in...' : 'Sign In →'}
          </button>
        </form>

        <p style={{ color: '#4A6A8A', fontSize: '12px', textAlign: 'center', marginTop: '24px' }}>
          AAST Student Portal v1.0
        </p>
      </div>
    </div>
  );
};

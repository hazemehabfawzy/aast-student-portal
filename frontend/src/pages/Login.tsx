import { useState } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export const Login: React.FC = () => {
  const { isAuthenticated, loading } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  if (loading) {
    return (
      <div style={{
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #0D1B2A 0%, #1A2F45 50%, #0D1B2A 100%)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
        color: '#FFFFFF'
      }}>
        <div>Loading authentication...</div>
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleLogin = async () => {
    if (!username.trim() || !password) {
      setError('Please enter username and password');
      return;
    }
    setIsLoading(true);
    setError('');

    try {
      const response = await fetch(
        'http://localhost:8080/realms/student-portal' +
        '/protocol/openid-connect/token',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          },
          body: new URLSearchParams({
            grant_type: 'password',
            client_id: 'web-app',
            username: username.trim(),
            password: password,
            scope: 'openid profile email',
          }),
        }
      );

      if (response.ok) {
        const data = await response.json();
        // Store tokens - using same keys as existing Keycloak adapter
        localStorage.setItem('kc_token', data.access_token);
        localStorage.setItem('kc_refreshToken', data.refresh_token);
        // Also store with common key for API client
        localStorage.setItem('access_token', data.access_token);
        // Reload page to trigger auth check
        window.location.href = '/';
      } else {
        const err = await response.json().catch(() => ({}));
        if (response.status === 401) {
          setError('Invalid username or password');
        } else {
          setError(err.error_description || 'Login failed. Please try again.');
        }
      }
    } catch {
      setError('Cannot connect to server. Please check your connection.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div style={{
      minHeight: '100vh',
      background: 'linear-gradient(135deg, #0D1B2A 0%, #1A2F45 50%, #0D1B2A 100%)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    }}>
      <div style={{ width: '100%', maxWidth: '420px', padding: '0 24px' }}>

        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: '40px' }}>
          <div style={{
            width: '90px',
            height: '90px',
            borderRadius: '50%',
            background: '#1A3A5C',
            border: '2px solid #2A5A8A',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 20px',
            fontSize: '44px',
          }}>
            🎓
          </div>
          <h1 style={{
            color: '#FFFFFF',
            fontSize: '26px',
            fontWeight: '800',
            letterSpacing: '3px',
            margin: '0 0 6px',
          }}>
            AAST PORTAL
          </h1>
          <p style={{
            color: '#8AAAC8',
            fontSize: '13px',
            margin: 0,
            letterSpacing: '0.5px',
          }}>
            Computer Engineering Department
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: '#1A2F45',
          borderRadius: '20px',
          padding: '36px 32px',
          border: '1px solid #2A4A6A',
          boxShadow: '0 20px 60px rgba(0,0,0,0.4)',
        }}>
          <h2 style={{
            color: '#FFFFFF',
            fontSize: '22px',
            fontWeight: '700',
            margin: '0 0 6px',
          }}>
            Sign In
          </h2>
          <p style={{
            color: '#8AAAC8',
            fontSize: '13px',
            margin: '0 0 28px',
          }}>
            Enter your AAST portal credentials
          </p>

          {/* Username */}
          <div style={{ marginBottom: '18px' }}>
            <label style={{
              color: '#8AAAC8',
              fontSize: '12px',
              fontWeight: '600',
              letterSpacing: '0.5px',
              display: 'block',
              marginBottom: '8px',
              textTransform: 'uppercase',
            }}>
              Username
            </label>
            <div style={{ position: 'relative' }}>
              <span style={{
                position: 'absolute',
                left: '14px',
                top: '50%',
                transform: 'translateY(-50%)',
                fontSize: '16px',
                color: '#4A90E2',
                pointerEvents: 'none',
              }}>
                👤
              </span>
              <input
                type="text"
                value={username}
                onChange={e => setUsername(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleLogin()}
                placeholder="e.g. student.one"
                autoComplete="username"
                style={{
                  width: '100%',
                  padding: '13px 14px 13px 44px',
                  background: '#0D1B2A',
                  border: '1.5px solid #2A4A6A',
                  borderRadius: '10px',
                  color: '#FFFFFF',
                  fontSize: '14px',
                  outline: 'none',
                  boxSizing: 'border-box',
                  transition: 'border-color 0.2s',
                }}
              />
            </div>
          </div>

          {/* Password */}
          <div style={{ marginBottom: '28px' }}>
            <label style={{
              color: '#8AAAC8',
              fontSize: '12px',
              fontWeight: '600',
              letterSpacing: '0.5px',
              display: 'block',
              marginBottom: '8px',
              textTransform: 'uppercase',
            }}>
              Password
            </label>
            <div style={{ position: 'relative' }}>
              <span style={{
                position: 'absolute',
                left: '14px',
                top: '50%',
                transform: 'translateY(-50%)',
                fontSize: '16px',
                color: '#4A90E2',
                pointerEvents: 'none',
              }}>
                🔒
              </span>
              <input
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={e => setPassword(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleLogin()}
                placeholder="Enter your password"
                autoComplete="current-password"
                style={{
                  width: '100%',
                  padding: '13px 44px 13px 44px',
                  background: '#0D1B2A',
                  border: '1.5px solid #2A4A6A',
                  borderRadius: '10px',
                  color: '#FFFFFF',
                  fontSize: '14px',
                  outline: 'none',
                  boxSizing: 'border-box',
                }}
              />
              <button
                onClick={() => setShowPassword(!showPassword)}
                style={{
                  position: 'absolute',
                  right: '12px',
                  top: '50%',
                  transform: 'translateY(-50%)',
                  background: 'none',
                  border: 'none',
                  color: '#4A90E2',
                  cursor: 'pointer',
                  fontSize: '18px',
                  padding: '4px',
                  lineHeight: 1,
                }}
              >
                {showPassword ? '🙈' : '👁️'}
              </button>
            </div>
          </div>

          {/* Error */}
          {error && (
            <div style={{
              background: 'rgba(244,67,54,0.1)',
              border: '1px solid rgba(244,67,54,0.4)',
              borderRadius: '10px',
              padding: '12px 14px',
              marginBottom: '20px',
              color: '#FF6B6B',
              fontSize: '13px',
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
            }}>
              ⚠️ {error}
            </div>
          )}

          {/* Sign In Button */}
          <button
            onClick={handleLogin}
            disabled={isLoading}
            style={{
              width: '100%',
              padding: '15px',
              background: isLoading
                ? '#2A4A6A'
                : 'linear-gradient(135deg, #4A90E2, #357ABD)',
              border: 'none',
              borderRadius: '10px',
              color: '#FFFFFF',
              fontSize: '16px',
              fontWeight: '700',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              letterSpacing: '0.5px',
              boxShadow: isLoading
                ? 'none'
                : '0 4px 15px rgba(74,144,226,0.4)',
              transition: 'all 0.2s',
            }}
          >
            {isLoading ? '⏳ Signing in...' : 'Sign In →'}
          </button>
        </div>

        {/* Footer */}
        <p style={{
          textAlign: 'center',
          color: '#4A6A8A',
          fontSize: '11px',
          marginTop: '24px',
          letterSpacing: '0.5px',
        }}>
          AAST Student Portal v1.0 — Computer Engineering
        </p>
      </div>
    </div>
  );
};

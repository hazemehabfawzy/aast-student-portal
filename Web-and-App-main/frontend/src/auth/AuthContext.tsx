import React, { createContext, useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

interface AuthContextType {
  isAuthenticated: boolean;
  token: string | null;
  username: string | null;
  email: string | null;
  role: string | null;
  fullName: string | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

let globalToken: string | null = null;
export const getGlobalToken = () => globalToken;

function parseJwt(token: string): Record<string, any> {
  const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
  return JSON.parse(atob(padded));
}

function applyTokenPayload(
  payload: Record<string, any>,
  setters: {
    setUsername: (v: string | null) => void;
    setEmail: (v: string | null) => void;
    setFullName: (v: string | null) => void;
    setRole: (v: string | null) => void;
  }
) {
  setters.setUsername(payload.preferred_username ?? null);
  setters.setEmail(payload.email ?? null);
  setters.setFullName(payload.name ?? payload.preferred_username ?? null);

  const roles: string[] = payload.realm_access?.roles ?? [];
  if (roles.includes('admin')) {
    setters.setRole('admin');
  } else if (roles.includes('instructor')) {
    setters.setRole('instructor');
  } else if (roles.includes('student')) {
    setters.setRole('student');
  } else {
    setters.setRole(null);
  }
}

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const navigate = useNavigate();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [token, setToken] = useState<string | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [role, setRole] = useState<string | null>(null);
  const [fullName, setFullName] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    globalToken = token;
  }, [token]);

  useEffect(() => {
    const saved = localStorage.getItem('access_token');
    if (saved) {
      try {
        const payload = parseJwt(saved);
        setToken(saved);
        setIsAuthenticated(true);
        applyTokenPayload(payload, { setUsername, setEmail, setFullName, setRole });
      } catch {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
      }
    }
    setLoading(false);
  }, []);

  const login = async (usernameInput: string, password: string) => {
    const params = new URLSearchParams({
      grant_type: 'password',
      client_id: 'web-app',
      username: usernameInput,
      password,
    });

    const resp = await fetch(
      'http://localhost:8080/realms/student-portal/protocol/openid-connect/token',
      {
        method: 'POST',
        body: params,
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      }
    );

    if (!resp.ok) throw new Error('Login failed');

    const data = await resp.json();
    setToken(data.access_token);
    localStorage.setItem('access_token', data.access_token);
    localStorage.setItem('refresh_token', data.refresh_token);
    setIsAuthenticated(true);

    const payload = parseJwt(data.access_token);
    applyTokenPayload(payload, { setUsername, setEmail, setFullName, setRole });
  };

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    setToken(null);
    setIsAuthenticated(false);
    setUsername(null);
    setEmail(null);
    setFullName(null);
    setRole(null);
    navigate('/login');
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        token,
        username,
        email,
        role,
        fullName,
        login,
        logout,
        loading,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

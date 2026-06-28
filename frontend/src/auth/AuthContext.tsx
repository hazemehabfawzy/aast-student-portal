import React, { createContext, useContext, useEffect, useState } from 'react';

interface AuthContextType {
  isAuthenticated: boolean;
  token: string | null;
  username: string | null;
  email: string | null;
  role: string | null; // admin, instructor, student
  fullName: string | null;
  login: () => void;
  logout: () => void;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

let globalToken: string | null = null;
export const getGlobalToken = () => globalToken;

const parseJwt = (token: string) => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    return null;
  }
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
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
    const storedToken = localStorage.getItem('access_token') || localStorage.getItem('kc_token');
    
    if (storedToken) {
      const decoded = parseJwt(storedToken);
      if (decoded && decoded.exp && decoded.exp * 1000 > Date.now()) {
        setToken(storedToken);
        setIsAuthenticated(true);
        setUsername(decoded.preferred_username ?? null);
        setEmail(decoded.email ?? null);
        setFullName(decoded.name ?? null);

        const realmRoles = decoded.realm_access?.roles || [];
        if (realmRoles.includes('admin')) {
          setRole('admin');
        } else if (realmRoles.includes('instructor')) {
          setRole('instructor');
        } else if (realmRoles.includes('student')) {
          setRole('student');
        }
      } else {
        // Token expired or invalid
        localStorage.removeItem('access_token');
        localStorage.removeItem('kc_token');
        localStorage.removeItem('kc_refreshToken');
      }
    }
    setLoading(false);
  }, []);

  const login = () => {
    window.location.href = '/login';
  };

  const logout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('kc_token');
    localStorage.removeItem('kc_refreshToken');
    setIsAuthenticated(false);
    setToken(null);
    setUsername(null);
    setEmail(null);
    setFullName(null);
    setRole(null);
    window.location.href = '/';
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

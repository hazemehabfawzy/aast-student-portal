import React, { createContext, useContext, useEffect, useState, useRef } from 'react';
import Keycloak from 'keycloak-js';

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

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [token, setToken] = useState<string | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [role, setRole] = useState<string | null>(null);
  const [fullName, setFullName] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const keycloakRef = useRef<Keycloak | null>(null);

  useEffect(() => {
    globalToken = token;
  }, [token]);


  useEffect(() => {
    const keycloak = new Keycloak({
      url: 'http://localhost:8080',
      realm: 'student-portal',
      clientId: 'web-app',
    });

    keycloakRef.current = keycloak;

    keycloak
      .init({
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
        pkceMethod: 'S256',
      })
      .then((authenticated) => {
        setIsAuthenticated(authenticated);
        if (authenticated) {
          setToken(keycloak.token ?? null);
          setUsername(keycloak.tokenParsed?.preferred_username ?? null);
          setEmail(keycloak.tokenParsed?.email ?? null);
          setFullName(keycloak.tokenParsed?.name ?? null);

          // Extract role
          const realmRoles = keycloak.tokenParsed?.realm_access?.roles || [];
          if (realmRoles.includes('admin')) {
            setRole('admin');
          } else if (realmRoles.includes('instructor')) {
            setRole('instructor');
          } else if (realmRoles.includes('student')) {
            setRole('student');
          }
        }
        setLoading(false);
      })
      .catch((err) => {
        console.error('Keycloak init error', err);
        setLoading(false);
      });

    // Refresh token automatically
    keycloak.onTokenExpired = () => {
      keycloak.updateToken(30).then((refreshed) => {
        if (refreshed) {
          setToken(keycloak.token ?? null);
        }
      });
    };
  }, []);

  const login = () => {
    keycloakRef.current?.login();
  };

  const logout = () => {
    keycloakRef.current?.logout({ redirectUri: window.location.origin });
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

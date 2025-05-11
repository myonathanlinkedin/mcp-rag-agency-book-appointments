'use client';

import React, { createContext, useContext, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/store/authStore';
import api from '@/lib/api';

interface AuthContextType {
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  user: { id: string; email: string } | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const router = useRouter();
  const { isAuthenticated, user, setTokens, clearTokens } = useAuthStore();

  useEffect(() => {
    // Set up token refresh interval
    const refreshInterval = setInterval(async () => {
      if (isAuthenticated) {
        try {
          const response = await api.post('/auth/refresh');
          const { accessToken } = response.data;
          useAuthStore.getState().updateAccessToken(accessToken);
        } catch (error) {
          console.error('Token refresh failed:', error);
          clearTokens();
          router.push('/login');
        }
      }
    }, 5 * 60 * 1000); // 5 minutes

    return () => clearInterval(refreshInterval);
  }, [isAuthenticated, clearTokens, router]);

  const login = async (email: string, password: string) => {
    try {
      const response = await api.post('/auth/login', { email, password });
      const { accessToken, refreshToken } = response.data;
      setTokens({ accessToken, refreshToken });
      router.push('/chat');
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  };

  const logout = () => {
    clearTokens();
    router.push('/login');
  };

  return (
    <AuthContext.Provider value={{ login, logout, isAuthenticated, user }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}; 
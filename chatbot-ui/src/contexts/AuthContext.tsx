'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/store/authStore';
import api from '@/lib/api';

interface AuthContextType {
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
  user: { id: string; email: string } | null;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const router = useRouter();
  const { isAuthenticated, user, setTokens, clearTokens, accessToken, refreshToken } = useAuthStore();
  const [isLoading, setIsLoading] = useState(true);

  // Restore auth state on mount
  useEffect(() => {
    const restoreAuth = async () => {
      if (!accessToken || !refreshToken) {
        setIsLoading(false);
        return;
      }

      try {
        // Set auth header for the request
        api.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
        
        // Only verify token if we're not on the login page
        if (window.location.pathname !== '/login') {
          const response = await api.post('/auth/verify');
          if (!response.data.valid && refreshToken) {
            // If token is invalid, try to refresh
            const refreshResponse = await api.post('/auth/refresh');
            const { accessToken: newAccessToken } = refreshResponse.data;
            useAuthStore.getState().updateAccessToken(newAccessToken);
            api.defaults.headers.common['Authorization'] = `Bearer ${newAccessToken}`;
          }
        }
      } catch (error) {
        console.error('Auth restoration failed:', error);
        // Only clear tokens and redirect if we're not on the login page
        if (window.location.pathname !== '/login') {
          clearTokens();
          router.push('/login');
        }
      } finally {
        setIsLoading(false);
      }
    };

    restoreAuth();
  }, [accessToken, refreshToken, clearTokens, router]);

  // Set up token refresh interval
  useEffect(() => {
    if (!isAuthenticated || !refreshToken) return;

    const refreshInterval = setInterval(async () => {
      try {
        const response = await api.post('/auth/refresh');
        const { accessToken: newAccessToken } = response.data;
        useAuthStore.getState().updateAccessToken(newAccessToken);
        api.defaults.headers.common['Authorization'] = `Bearer ${newAccessToken}`;
      } catch (error) {
        console.error('Token refresh failed:', error);
        // Only clear tokens and redirect if we're not on the login page
        if (window.location.pathname !== '/login') {
          clearTokens();
          router.push('/login');
        }
      }
    }, 5 * 60 * 1000); // 5 minutes

    return () => clearInterval(refreshInterval);
  }, [isAuthenticated, refreshToken, clearTokens, router]);

  const login = async (email: string, password: string) => {
    try {
      setIsLoading(true);
      const response = await api.post('/auth/login', { email, password });
      const { accessToken, refreshToken } = response.data;
      
      // Set auth header
      api.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
      
      setTokens({ accessToken, refreshToken });
      router.push('/chat');
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      setIsLoading(true);
      // Only call logout endpoint if we have a token
      if (accessToken) {
        await api.post('/auth/logout');
      }
    } catch (error) {
      console.error('Logout failed:', error);
    } finally {
      clearTokens();
      delete api.defaults.headers.common['Authorization'];
      router.push('/login');
      setIsLoading(false);
    }
  };

  return (
    <AuthContext.Provider value={{ login, logout, isAuthenticated, user, isLoading }}>
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
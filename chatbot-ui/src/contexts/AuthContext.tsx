'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/store/authStore';
import api from '@/lib/api';

interface AuthContextType {
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  register: (email: string, password: string) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  resetPassword: (email: string) => Promise<void>;
  isAuthenticated: boolean;
  user: { id: string; email: string } | null;
  isLoading: boolean;
}

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const { accessToken, refreshToken, setTokens, clearTokens } = useAuthStore();
  const [user, setUser] = useState<{ id: string; email: string } | null>(null);
  const isAuthenticated = !!accessToken;

  // Initialize auth state from stored tokens
  useEffect(() => {
    if (accessToken) {
      api.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
      // Extract user info from token
      try {
        const base64Url = accessToken.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        const payload = JSON.parse(jsonPayload);
        setUser({
          id: payload.nameid,
          email: payload.unique_name
        });
      } catch (error) {
        console.error('Failed to decode token:', error);
        clearTokens();
      }
    }
  }, [accessToken, clearTokens]);

  // Set up token refresh interval
  useEffect(() => {
    if (!isAuthenticated || !refreshToken) return;

    const refreshInterval = setInterval(async () => {
      try {
        const response = await api.post('/api/Identity/RefreshToken/RefreshTokenAsync', {
          userId: user?.id,
          refreshToken
        });
        const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;
        setTokens({ accessToken: newAccessToken, refreshToken: newRefreshToken });
        api.defaults.headers.common['Authorization'] = `Bearer ${newAccessToken}`;
      } catch (error) {
        console.error('Token refresh failed:', error);
        if (window.location.pathname !== '/login') {
          clearTokens();
          router.push('/login');
        }
      }
    }, 4 * 60 * 1000); // 4 minutes (token expires in 5 minutes)

    return () => clearInterval(refreshInterval);
  }, [isAuthenticated, refreshToken, user?.id, setTokens, clearTokens, router]);

  const login = async (email: string, password: string) => {
    try {
      setIsLoading(true);
      const response = await api.post<TokenResponse>('/api/Identity/Login/LoginAsync', {
        email,
        password
      });
      
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
      if (accessToken) {
        // No need to call logout endpoint as we're using JWT
        clearTokens();
        delete api.defaults.headers.common['Authorization'];
        setUser(null);
      }
    } catch (error) {
      console.error('Logout failed:', error);
    } finally {
      router.push('/login');
      setIsLoading(false);
    }
  };

  const register = async (email: string, password: string) => {
    try {
      setIsLoading(true);
      await api.post('/api/Identity/Register/RegisterAsync', {
        email,
        password,
        confirmPassword: password
      });
      // After successful registration, redirect to login
      router.push('/login');
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const changePassword = async (currentPassword: string, newPassword: string) => {
    try {
      setIsLoading(true);
      await api.put('/api/Identity/ChangePassword/ChangePasswordAsync', {
        currentPassword,
        newPassword
      });
    } catch (error) {
      console.error('Password change failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const resetPassword = async (email: string) => {
    try {
      setIsLoading(true);
      await api.post('/api/Identity/ResetPassword/ResetPasswordAsync', {
        email
      });
    } catch (error) {
      console.error('Password reset failed:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthContext.Provider value={{ 
      login, 
      logout, 
      register, 
      changePassword, 
      resetPassword, 
      isAuthenticated, 
      user, 
      isLoading 
    }}>
      {children}
    </AuthContext.Provider>
  );
}; 
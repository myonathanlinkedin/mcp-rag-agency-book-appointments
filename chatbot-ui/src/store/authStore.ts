import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { jwtDecode } from 'jwt-decode';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  user: {
    id: string;
    email: string;
  } | null;
  setTokens: (tokens: { accessToken: string; refreshToken: string }) => void;
  clearTokens: () => void;
  updateAccessToken: (accessToken: string) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      user: null,
      setTokens: (tokens) => {
        const decoded = jwtDecode(tokens.accessToken) as { sub: string; email: string };
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          isAuthenticated: true,
          user: {
            id: decoded.sub,
            email: decoded.email,
          },
        });
      },
      clearTokens: () => {
        set({
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          user: null,
        });
      },
      updateAccessToken: (accessToken) => {
        const decoded = jwtDecode(accessToken) as { sub: string; email: string };
        set({
          accessToken,
          isAuthenticated: true,
          user: {
            id: decoded.sub,
            email: decoded.email,
          },
        });
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
      }),
    }
  )
); 
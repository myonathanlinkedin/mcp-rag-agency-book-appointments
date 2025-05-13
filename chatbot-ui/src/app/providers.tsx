'use client';

import { ChakraProvider, ColorModeScript, extendTheme } from '@chakra-ui/react';
import { AuthProvider } from '@/contexts/AuthContext';

const theme = extendTheme({
  colors: {
    brand: {
      50: '#E6F6FF',
      100: '#BAE3FF',
      200: '#7CC4FA',
      300: '#47A3F3',
      400: '#2186EB',
      500: '#0967D2',
      600: '#0552B5',
      700: '#03449E',
      800: '#01337D',
      900: '#002159',
    },
    linkedin: {
      dark: {
        bg: '#1A1D21',
        card: '#282B30',
        border: '#404040',
        text: '#E7E9EA',
        input: '#1E2024',
        hover: '#363A3F',
        message: {
          user: '#0552B5',
          assistant: '#282B30',
        },
      },
      light: {
        bg: '#F3F2EF',
        card: '#FFFFFF',
        border: '#E0E0E0',
        text: '#1A1D21',
        input: '#FFFFFF',
        hover: '#F5F5F5',
        message: {
          user: '#0552B5',
          assistant: '#FFFFFF',
        },
        messageText: {
          user: '#FFFFFF',
          assistant: '#1A1D21'
        }
      },
    },
  },
  styles: {
    global: {
      body: {
        bg: 'linkedin.light.bg',
        color: 'gray.800',
      },
    },
  },
  config: {
    initialColorMode: 'light',
    useSystemColorMode: false,
  },
});

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <>
      <ColorModeScript initialColorMode="light" />
      <ChakraProvider theme={theme}>
        <AuthProvider>{children}</AuthProvider>
      </ChakraProvider>
    </>
  );
} 
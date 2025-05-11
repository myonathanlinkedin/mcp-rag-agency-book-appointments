'use client';

import { extendTheme, type ThemeConfig } from '@chakra-ui/react';

const config = {
  initialColorMode: 'light',
  useSystemColorMode: true,
} as const;

const theme = extendTheme({
  config,
  fonts: {
    heading: 'var(--font-inter)',
    body: 'var(--font-inter)',
    mono: 'var(--font-inter)',
  },
  colors: {
    brand: {
      50: '#f0f9ff',
      100: '#e0f2fe',
      200: '#bae6fd',
      300: '#7dd3fc',
      400: '#38bdf8',
      500: '#0ea5e9',
      600: '#0284c7',
      700: '#0369a1',
      800: '#075985',
      900: '#0c4a6e',
    },
    gray: {
      50: '#f8fafc',
      100: '#f1f5f9',
      200: '#e2e8f0',
      300: '#cbd5e1',
      400: '#94a3b8',
      500: '#64748b',
      600: '#475569',
      700: '#334155',
      800: '#1e293b',
      900: '#0f172a',
    },
    // LinkedIn dark mode colors
    linkedin: {
      dark: {
        bg: '#1a1a1a',
        card: '#2d2d2d',
        text: '#e6e6e6',
        border: '#404040',
        hover: '#404040',
        input: '#2d2d2d',
        message: {
          user: '#0a66c2',
          assistant: '#2d2d2d',
        },
        softBlue: '#0a66c2',
      },
      light: {
        bg: '#ffffff',
        card: '#ffffff',
        text: '#000000',
        border: '#e2e8f0',
        hover: '#f1f5f9',
        input: '#ffffff',
        message: {
          user: '#0a66c2',
          assistant: '#f1f5f9',
        },
        softBlue: '#0a66c2',
      },
    },
  },
  components: {
    Button: {
      baseStyle: {
        fontWeight: 'semibold',
        borderRadius: 'md',
      },
      variants: {
        solid: {
          bg: 'brand.600',
          color: 'white',
          _hover: {
            bg: 'brand.700',
          },
        },
        ghost: {
          _hover: {
            bg: 'gray.100',
          },
        },
      },
      defaultProps: {
        colorScheme: 'brand',
      },
    },
    Input: {
      baseStyle: {
        field: {
          _focus: {
            borderColor: 'brand.500',
            boxShadow: '0 0 0 1px var(--chakra-colors-brand-500)',
          },
        },
      },
      defaultProps: {
        focusBorderColor: 'brand.500',
      },
    },
    Heading: {
      baseStyle: {
        fontWeight: 'bold',
        letterSpacing: 'tight',
      },
    },
    Text: {
      baseStyle: {
        color: 'gray.700',
      },
    },
  },
  styles: {
    global: (props: { colorMode: string }) => ({
      body: {
        bg: props.colorMode === 'dark' ? 'linkedin.dark.bg' : 'linkedin.light.bg',
        color: props.colorMode === 'dark' ? 'linkedin.dark.text' : 'linkedin.light.text',
      },
    }),
  },
});

export default theme; 
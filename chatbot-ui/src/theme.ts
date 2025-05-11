'use client';

import { extendTheme, type ThemeConfig } from '@chakra-ui/react';

const config = {
  initialColorMode: 'light',
  useSystemColorMode: false,
} as const;

const theme = extendTheme({
  config,
  fonts: {
    heading: 'var(--font-inter)',
    body: 'var(--font-inter)',
    mono: 'var(--font-inter)',
  },
  components: {
    Button: {
      defaultProps: {
        colorScheme: 'blue',
      },
    },
    Input: {
      defaultProps: {
        focusBorderColor: 'blue.500',
      },
    },
  },
  styles: {
    global: {
      body: {
        bg: 'gray.50',
        color: 'gray.900',
      },
    },
  },
});

export default theme; 
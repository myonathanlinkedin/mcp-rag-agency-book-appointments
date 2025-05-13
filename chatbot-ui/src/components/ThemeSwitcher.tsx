'use client';

import { IconButton, useColorMode } from '@chakra-ui/react';
import { FiSun, FiMoon } from 'react-icons/fi';

export function ThemeSwitcher() {
  const { colorMode, toggleColorMode } = useColorMode();

  return (
    <IconButton
      aria-label="Toggle color mode"
      icon={colorMode === 'dark' ? <FiSun /> : <FiMoon />}
      onClick={toggleColorMode}
      variant="ghost"
      colorScheme="gray"
      size="sm"
      _hover={{
        bg: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.100',
      }}
    />
  );
} 
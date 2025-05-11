'use client';

import { IconButton, useColorMode, Tooltip } from '@chakra-ui/react';
import { FiSun, FiMoon } from 'react-icons/fi';

export function ThemeSwitcher() {
  const { colorMode, toggleColorMode } = useColorMode();

  return (
    <Tooltip
      label={colorMode === 'light' ? 'Switch to dark mode' : 'Switch to light mode'}
      placement="bottom"
      hasArrow
    >
      <IconButton
        aria-label="Toggle color mode"
        icon={colorMode === 'light' ? <FiMoon /> : <FiSun />}
        variant="ghost"
        colorScheme="gray"
        size="sm"
        onClick={toggleColorMode}
        _hover={{
          bg: colorMode === 'light' ? 'gray.100' : 'linkedin.dark.hover',
        }}
      />
    </Tooltip>
  );
} 
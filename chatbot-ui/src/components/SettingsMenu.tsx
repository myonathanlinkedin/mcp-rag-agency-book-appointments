'use client';

import {
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  IconButton,
  useColorMode,
  Icon,
} from '@chakra-ui/react';
import { FiSettings, FiLock, FiLogOut } from 'react-icons/fi';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';

export function SettingsMenu() {
  const router = useRouter();
  const { logout } = useAuth();
  const { colorMode } = useColorMode();

  const handleChangePassword = () => {
    router.push('/change-password');
  };

  return (
    <Menu>
      <MenuButton
        as={IconButton}
        aria-label="Settings"
        icon={<FiSettings />}
        variant="ghost"
        colorScheme="gray"
        size="sm"
        _hover={{
          bg: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.100',
        }}
      />
      <MenuList
        bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'white'}
        borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'gray.200'}
        boxShadow="sm"
      >
        <MenuItem
          icon={<Icon as={FiLock} />}
          onClick={handleChangePassword}
          _hover={{
            bg: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.100',
          }}
          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
        >
          Change Password
        </MenuItem>
        <MenuItem
          icon={<Icon as={FiLogOut} />}
          onClick={logout}
          _hover={{
            bg: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.100',
          }}
          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
        >
          Logout
        </MenuItem>
      </MenuList>
    </Menu>
  );
} 
import React, { useEffect, useState } from 'react';
import {
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  Text,
  Box,
  useColorMode,
  VStack,
  HStack,
  Icon,
} from '@chakra-ui/react';
import { FiInfo } from 'react-icons/fi';

interface TipsPopupProps {
  isOpen: boolean;
  onClose: () => void;
}

const TIPS_COOKIE_KEY = 'chat_tips_shown';

export const TipsPopup: React.FC<TipsPopupProps> = ({ isOpen, onClose }) => {
  const { colorMode } = useColorMode();

  const handleDontShowAgain = () => {
    // Set cookie to expire in 365 days
    const expiryDate = new Date();
    expiryDate.setDate(expiryDate.getDate() + 365);
    document.cookie = `${TIPS_COOKIE_KEY}=true; expires=${expiryDate.toUTCString()}; path=/`;
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} isCentered size="md">
      <ModalOverlay bg="blackAlpha.300" backdropFilter="blur(5px)" />
      <ModalContent
        bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
        borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
        borderWidth="1px"
        boxShadow="xl"
      >
        <ModalHeader
          borderBottomWidth="1px"
          borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
        >
          <HStack spacing={2}>
            <Icon as={FiInfo} color="brand.500" boxSize={5} />
            <Text color={colorMode === 'dark' ? 'white' : 'gray.800'}>Quick Tip</Text>
          </HStack>
        </ModalHeader>
        <ModalBody py={6}>
          <VStack spacing={4} align="start">
            <Text 
              fontSize="md" 
              fontWeight="medium"
              color={colorMode === 'dark' ? 'white' : 'gray.800'}
            >
              Use prompt key: "Rag search" to search internal document knowledge
            </Text>
            <Box
              p={4}
              bg={colorMode === 'dark' ? 'whiteAlpha.100' : 'gray.50'}
              borderRadius="md"
              width="100%"
              borderWidth="1px"
              borderColor={colorMode === 'dark' ? 'whiteAlpha.200' : 'gray.200'}
            >
              <Text 
                fontSize="sm" 
                color={colorMode === 'dark' ? 'gray.200' : 'gray.600'}
                fontFamily="mono"
              >
                Example: "Rag search: What are our company policies?"
              </Text>
            </Box>
          </VStack>
        </ModalBody>
        <ModalFooter
          borderTopWidth="1px"
          borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
        >
          <Button
            variant="ghost"
            mr={3}
            onClick={onClose}
            color={colorMode === 'dark' ? 'gray.300' : 'gray.600'}
            _hover={{
              bg: colorMode === 'dark' ? 'whiteAlpha.100' : 'gray.100'
            }}
          >
            Got it
          </Button>
          <Button
            colorScheme="brand"
            onClick={handleDontShowAgain}
          >
            Don't show again
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
};

export const shouldShowTips = (): boolean => {
  const cookies = document.cookie.split(';');
  const tipsCookie = cookies.find(cookie => cookie.trim().startsWith(`${TIPS_COOKIE_KEY}=`));
  return !tipsCookie;
}; 
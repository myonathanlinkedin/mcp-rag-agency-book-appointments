'use client';

import React from 'react';
import { Box, Text, Icon, VStack, Button } from '@chakra-ui/react';
import { FiAlertCircle } from 'react-icons/fi';

interface ErrorMessageProps {
  title?: string;
  message: string;
  onRetry?: () => void;
  showRetry?: boolean;
}

const ErrorMessage: React.FC<ErrorMessageProps> = ({
  title = 'Error',
  message,
  onRetry,
  showRetry = false,
}) => {
  return (
    <Box
      p={4}
      borderRadius="md"
      bg="red.50"
      border="1px"
      borderColor="red.200"
      role="alert"
    >
      <VStack spacing={3} align="start">
        <Box display="flex" alignItems="center">
          <Icon as={FiAlertCircle} color="red.500" mr={2} />
          <Text fontWeight="bold" color="red.700">
            {title}
          </Text>
        </Box>
        <Text color="red.600">{message}</Text>
        {showRetry && onRetry && (
          <Button
            size="sm"
            colorScheme="red"
            variant="outline"
            onClick={onRetry}
          >
            Try Again
          </Button>
        )}
      </VStack>
    </Box>
  );
};

export default ErrorMessage; 
'use client';

import React from 'react';
import { Center, Spinner, Text, VStack } from '@chakra-ui/react';

interface LoadingStateProps {
  message?: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
}

const LoadingState: React.FC<LoadingStateProps> = ({
  message = 'Loading...',
  size = 'md',
}) => {
  return (
    <Center p={8}>
      <VStack spacing={4}>
        <Spinner
          thickness="4px"
          speed="0.65s"
          emptyColor="gray.200"
          color="blue.500"
          size={size}
        />
        <Text color="gray.600" fontSize="sm">
          {message}
        </Text>
      </VStack>
    </Center>
  );
};

export default LoadingState; 
'use client';

import React from 'react';
import { Box, Heading, Text, Button, VStack } from '@chakra-ui/react';
import { FiAlertTriangle } from 'react-icons/fi';

interface Props {
  children: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <Box
          minH="100vh"
          display="flex"
          alignItems="center"
          justifyContent="center"
          p={4}
        >
          <VStack spacing={4} textAlign="center">
            <FiAlertTriangle size={48} color="red" />
            <Heading size="lg">Something went wrong</Heading>
            <Text color="gray.600">
              {this.state.error?.message || 'An unexpected error occurred'}
            </Text>
            <Button
              colorScheme="blue"
              onClick={() => {
                this.setState({ hasError: false, error: null });
                window.location.reload();
              }}
            >
              Try again
            </Button>
          </VStack>
        </Box>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary; 
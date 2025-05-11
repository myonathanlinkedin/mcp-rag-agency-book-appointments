'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Input,
  VStack,
  Heading,
  Text,
  useToast,
  Container,
  FormErrorMessage,
  Flex,
  Image,
  InputGroup,
  InputLeftElement,
  Divider,
  useColorModeValue,
} from '@chakra-ui/react';
import { FiMail, FiLock, FiLogIn } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const toast = useToast();
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    try {
      await login(data.email, data.password);
    } catch (error) {
      toast({
        title: 'Login failed',
        description: 'Invalid email or password',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Flex minH="100vh" bg="gray.50" align="center" justify="center" p={4}>
      <Container maxW="container.sm">
        <Flex
          direction={{ base: 'column', md: 'row' }}
          bg={bgColor}
          borderRadius="xl"
          boxShadow="xl"
          overflow="hidden"
        >
          {/* Left side - Branding */}
          <Box
            w={{ base: 'full', md: '40%' }}
            bg="brand.600"
            p={8}
            display={{ base: 'none', md: 'flex' }}
            flexDirection="column"
            justifyContent="center"
            alignItems="center"
            color="white"
          >
            <Image
              src="/logo-placeholder.svg"
              alt="Agent Book Logo"
              w="120px"
              h="120px"
              mb={6}
            />
            <Heading size="lg" mb={4} textAlign="center">
              Agent Book
            </Heading>
            <Text textAlign="center" opacity={0.9}>
              Your intelligent appointment booking assistant
            </Text>
          </Box>

          {/* Right side - Login Form */}
          <Box w={{ base: 'full', md: '60%' }} p={8}>
            <VStack spacing={6} align="stretch">
              <Box textAlign="center" mb={8}>
                <Heading size="lg" mb={2}>
                  Welcome Back
                </Heading>
                <Text color="gray.600">
                  Sign in to access your dashboard
                </Text>
              </Box>

              <form onSubmit={handleSubmit(onSubmit)}>
                <VStack spacing={5}>
                  <FormControl isInvalid={!!errors.email}>
                    <FormLabel fontWeight="medium">Email</FormLabel>
                    <InputGroup>
                      <InputLeftElement pointerEvents="none">
                        <FiMail color="gray.400" />
                      </InputLeftElement>
                      <Input
                        type="email"
                        {...register('email')}
                        placeholder="Enter your email"
                        size="lg"
                        bg="gray.50"
                      />
                    </InputGroup>
                    <FormErrorMessage>
                      {errors.email && errors.email.message}
                    </FormErrorMessage>
                  </FormControl>

                  <FormControl isInvalid={!!errors.password}>
                    <FormLabel fontWeight="medium">Password</FormLabel>
                    <InputGroup>
                      <InputLeftElement pointerEvents="none">
                        <FiLock color="gray.400" />
                      </InputLeftElement>
                      <Input
                        type="password"
                        {...register('password')}
                        placeholder="Enter your password"
                        size="lg"
                        bg="gray.50"
                      />
                    </InputGroup>
                    <FormErrorMessage>
                      {errors.password && errors.password.message}
                    </FormErrorMessage>
                  </FormControl>

                  <Button
                    type="submit"
                    size="lg"
                    width="full"
                    isLoading={isLoading}
                    leftIcon={<FiLogIn />}
                    mt={4}
                  >
                    Sign In
                  </Button>
                </VStack>
              </form>

              <Divider my={6} />

              <Text textAlign="center" fontSize="sm" color="gray.600">
                © {new Date().getFullYear()} Agent Book. All rights reserved.
              </Text>
            </VStack>
          </Box>
        </Flex>
      </Container>
    </Flex>
  );
} 
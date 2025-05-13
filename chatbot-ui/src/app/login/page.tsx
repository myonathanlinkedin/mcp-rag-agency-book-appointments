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
  Link,
  HStack,
  useColorMode,
  InputRightElement,
  IconButton,
} from '@chakra-ui/react';
import { FiMail, FiLock, FiLogIn, FiEye, FiEyeOff } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';
import { ThemeSwitcher } from '@/components/ThemeSwitcher';
import NextLink from 'next/link';

const loginSchema = z.object({
  email: z.string().refine((email) => {
    // Custom email validation that allows localhost
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$|^[a-zA-Z0-9._%+-]+@localhost$/;
    return emailRegex.test(email);
  }, 'Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const toast = useToast();
  const { colorMode } = useColorMode();
  const [show, setShow] = useState(false);

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
    <Flex 
      minH="100vh" 
      bg={colorMode === 'dark' ? 'linkedin.dark.bg' : 'linkedin.light.bg'} 
      align="center" 
      justify="center" 
      p={4}
    >
      <Container maxW="container.lg">
        <Flex
          direction={{ base: 'column', md: 'row' }}
          bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
          borderRadius="md"
          boxShadow="none"
          overflow="hidden"
          gap={12}
          maxW="1000px"
          mx="auto"
          position="relative"
        >
          {/* Theme Switcher */}
          <Box position="absolute" top={4} right={4}>
            <ThemeSwitcher />
          </Box>

          {/* Left side - Branding */}
          <Box
            w={{ base: 'full', md: '50%' }}
            bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
            p={12}
            display={{ base: 'none', md: 'flex' }}
            flexDirection="column"
            justifyContent="center"
            alignItems="flex-start"
          >
            <Image
              src="/logo-placeholder.svg"
              alt="Agent Book Logo"
              w="84px"
              h="84px"
              mb={8}
              bg={colorMode === 'dark' ? 'transparent' : 'brand.600'}
              p={2}
              borderRadius="md"
            />
            <Heading 
              size="lg" 
              mb={6} 
              color="brand.600"
              fontWeight="bold"
              letterSpacing="tight"
              lineHeight="1.2"
            >
              Welcome to your professional appointment assistant
            </Heading>
            <Text 
              color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
              fontSize="md"
              lineHeight="1.5"
            >
              Your intelligent appointment booking assistant
            </Text>
          </Box>

          {/* Right side - Login Form */}
          <Box 
            w={{ base: 'full', md: '50%' }}
            p={12}
            borderLeft={{ base: 'none', md: '1px' }}
            borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
          >
            <Box
              bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
              p={6}
              borderRadius="md"
              border="1px"
              borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
              boxShadow="sm"
            >
              <VStack spacing={6} align="stretch">
                <Box mb={6}>
                  <Heading 
                    size="lg" 
                    mb={2}
                    color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.900'}
                    fontWeight="bold"
                    letterSpacing="tight"
                  >
                    Sign in
                  </Heading>
                  <Text 
                    color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                    fontSize="sm"
                  >
                    Stay updated on your appointments
                  </Text>
                </Box>

                <form onSubmit={handleSubmit(onSubmit)}>
                  <VStack spacing={4}>
                    <FormControl isInvalid={!!errors.email}>
                      <FormLabel 
                        fontWeight="medium" 
                        fontSize="sm"
                        color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
                        mb={1}
                      >
                        Email
                      </FormLabel>
                      <InputGroup>
                        <InputLeftElement pointerEvents="none">
                          <FiMail color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.400'} />
                        </InputLeftElement>
                        <Input
                          type="email"
                          {...register('email')}
                          placeholder="Email"
                          size="md"
                          bg={colorMode === 'dark' ? 'linkedin.dark.input' : 'linkedin.light.input'}
                          fontSize="sm"
                          borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
                          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.900'}
                          _hover={{ 
                            borderColor: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.400' 
                          }}
                          _focus={{
                            borderColor: 'brand.500',
                            boxShadow: '0 0 0 1px var(--chakra-colors-brand-500)',
                          }}
                          h="40px"
                        />
                      </InputGroup>
                      <FormErrorMessage fontSize="xs">
                        {errors.email && errors.email.message}
                      </FormErrorMessage>
                    </FormControl>

                    <FormControl isInvalid={!!errors.password}>
                      <FormLabel 
                        fontWeight="medium" 
                        fontSize="sm"
                        color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
                        mb={1}
                      >
                        Password
                      </FormLabel>
                      <InputGroup>
                        <InputLeftElement pointerEvents="none">
                          <FiLock color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.400'} />
                        </InputLeftElement>
                        <Input
                          type={show ? "text" : "password"}
                          {...register('password')}
                          placeholder="Password"
                          size="md"
                          bg={colorMode === 'dark' ? 'linkedin.dark.input' : 'linkedin.light.input'}
                          fontSize="sm"
                          borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
                          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.900'}
                          _hover={{ 
                            borderColor: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.400' 
                          }}
                          _focus={{
                            borderColor: 'brand.500',
                            boxShadow: '0 0 0 1px var(--chakra-colors-brand-500)',
                          }}
                          h="40px"
                        />
                        <InputRightElement width="4.5rem">
                          <IconButton
                            aria-label={show ? 'Hide password' : 'Show password'}
                            h="1.75rem"
                            size="sm"
                            onClick={() => setShow(!show)}
                            icon={show ? <FiEyeOff /> : <FiEye />}
                            variant="ghost"
                            color={colorMode === 'dark' ? 'gray.400' : 'gray.600'}
                            _hover={{
                              bg: colorMode === 'dark' ? 'whiteAlpha.100' : 'gray.100'
                            }}
                          />
                        </InputRightElement>
                      </InputGroup>
                      <FormErrorMessage fontSize="xs">
                        {errors.password && errors.password.message}
                      </FormErrorMessage>
                    </FormControl>

                    <Button
                      type="submit"
                      colorScheme="brand"
                      size="md"
                      fontSize="sm"
                      fontWeight="semibold"
                      width="full"
                      h="40px"
                      isLoading={isLoading}
                      loadingText="Signing in..."
                    >
                      Sign in
                    </Button>

                    <HStack spacing={2} justify="space-between" mt={4}>
                      <Link
                        as={NextLink}
                        href="/register"
                        color="brand.500"
                        fontSize="sm"
                        fontWeight="semibold"
                        _hover={{ textDecoration: 'underline' }}
                      >
                        Create account
                      </Link>
                      <Link
                        as={NextLink}
                        href="/reset-password"
                        color="brand.500"
                        fontSize="sm"
                        fontWeight="semibold"
                        _hover={{ textDecoration: 'underline' }}
                      >
                        Forgot password?
                      </Link>
                    </HStack>
                  </VStack>
                </form>
              </VStack>
            </Box>

            <Text 
              textAlign="center" 
              fontSize="xs" 
              color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.500'}
              mt={6}
            >
              © 2025 Agent Book. All rights reserved. Developed by Mateus Yonathan.
            </Text>
          </Box>
        </Flex>
      </Container>
    </Flex>
  );
} 
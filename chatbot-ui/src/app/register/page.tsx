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
  useColorMode,
  Link,
} from '@chakra-ui/react';
import { FiMail, FiLock, FiUserPlus } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';
import { ThemeSwitcher } from '@/components/ThemeSwitcher';
import NextLink from 'next/link';

const registerSchema = z.object({
  email: z.string().refine((email) => {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$|^[a-zA-Z0-9._%+-]+@localhost$/;
    return emailRegex.test(email);
  }, 'Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
  confirmPassword: z.string().min(6, 'Password must be at least 6 characters'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
});

type RegisterFormData = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const [isLoading, setIsLoading] = useState(false);
  const { register: registerUser } = useAuth();
  const toast = useToast();
  const { colorMode } = useColorMode();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    setIsLoading(true);
    try {
      await registerUser(data.email, data.password);
      toast({
        title: 'Registration successful',
        description: 'Please check your email for verification instructions',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
    } catch (error) {
      toast({
        title: 'Registration failed',
        description: 'An error occurred during registration',
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
            />
            <Heading 
              size="lg" 
              mb={6} 
              color="brand.600"
              fontWeight="bold"
              letterSpacing="tight"
              lineHeight="1.2"
            >
              Join Agent Book Today
            </Heading>
            <Text 
              color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
              fontSize="md"
              lineHeight="1.5"
            >
              Create your account and start managing appointments efficiently
            </Text>
          </Box>

          {/* Right side - Register Form */}
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
                    Create Account
                  </Heading>
                  <Text 
                    color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                    fontSize="sm"
                  >
                    Fill in your details to get started
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
                          type="password"
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
                      </InputGroup>
                      <FormErrorMessage fontSize="xs">
                        {errors.password && errors.password.message}
                      </FormErrorMessage>
                    </FormControl>

                    <FormControl isInvalid={!!errors.confirmPassword}>
                      <FormLabel 
                        fontWeight="medium" 
                        fontSize="sm"
                        color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
                        mb={1}
                      >
                        Confirm Password
                      </FormLabel>
                      <InputGroup>
                        <InputLeftElement pointerEvents="none">
                          <FiLock color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.400'} />
                        </InputLeftElement>
                        <Input
                          type="password"
                          {...register('confirmPassword')}
                          placeholder="Confirm Password"
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
                        {errors.confirmPassword && errors.confirmPassword.message}
                      </FormErrorMessage>
                    </FormControl>

                    <Button
                      type="submit"
                      size="md"
                      width="full"
                      isLoading={isLoading}
                      leftIcon={<FiUserPlus />}
                      mt={2}
                      bg="brand.600"
                      color="white"
                      h="40px"
                      fontSize="sm"
                      fontWeight="semibold"
                      _hover={{
                        bg: 'brand.700',
                      }}
                      _active={{
                        bg: 'brand.800',
                      }}
                    >
                      Create Account
                    </Button>
                  </VStack>
                </form>

                <Text 
                  textAlign="center" 
                  fontSize="sm" 
                  color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                  mt={4}
                >
                  Already have an account?{' '}
                  <Link
                    as={NextLink}
                    href="/login"
                    color="brand.600"
                    fontWeight="semibold"
                    _hover={{
                      textDecoration: 'none',
                      color: 'brand.700',
                    }}
                  >
                    Sign in
                  </Link>
                </Text>
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
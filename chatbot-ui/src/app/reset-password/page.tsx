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
  useColorMode,
} from '@chakra-ui/react';
import { FiMail } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';
import { ThemeSwitcher } from '@/components/ThemeSwitcher';
import NextLink from 'next/link';

const resetPasswordSchema = z.object({
  email: z.string().refine((email) => {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$|^[a-zA-Z0-9._%+-]+@localhost$/;
    return emailRegex.test(email);
  }, 'Invalid email address'),
});

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export default function ResetPasswordPage() {
  const [isLoading, setIsLoading] = useState(false);
  const { resetPassword } = useAuth();
  const toast = useToast();
  const { colorMode } = useColorMode();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
  });

  const onSubmit = async (data: ResetPasswordFormData) => {
    setIsLoading(true);
    try {
      await resetPassword(data.email);
      toast({
        title: 'Reset password email sent',
        description: 'Please check your email for reset instructions',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
    } catch (error) {
      toast({
        title: 'Reset password failed',
        description: 'An error occurred while sending reset instructions',
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
              Reset your password
            </Heading>
            <Text 
              color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
              fontSize="md"
              lineHeight="1.5"
            >
              We'll send you instructions to reset your password
            </Text>
          </Box>

          {/* Right side - Reset Password Form */}
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
                    Reset Password
                  </Heading>
                  <Text 
                    color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                    fontSize="sm"
                  >
                    Enter your email to receive reset instructions
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
                      <FormErrorMessage>{errors.email?.message}</FormErrorMessage>
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
                      loadingText="Sending Instructions..."
                    >
                      Send Reset Instructions
                    </Button>

                    <Text 
                      fontSize="sm" 
                      color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                      textAlign="center"
                      mt={4}
                    >
                      Remember your password?{' '}
                      <Link
                        as={NextLink}
                        href="/login"
                        color="brand.500"
                        fontWeight="semibold"
                        _hover={{ textDecoration: 'underline' }}
                      >
                        Sign in
                      </Link>
                    </Text>
                  </VStack>
                </form>
              </VStack>
            </Box>
          </Box>
        </Flex>
      </Container>
    </Flex>
  );
} 
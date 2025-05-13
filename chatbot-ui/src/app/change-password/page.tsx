'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useRouter } from 'next/navigation';
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
} from '@chakra-ui/react';
import { FiLock } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';
import { ThemeSwitcher } from '@/components/ThemeSwitcher';
import { ProtectedRoute } from '@/components/ProtectedRoute';

const changePasswordSchema = z.object({
  currentPassword: z.string().min(6, 'Password must be at least 6 characters'),
  newPassword: z.string().min(6, 'Password must be at least 6 characters'),
  confirmNewPassword: z.string().min(6, 'Password must be at least 6 characters'),
}).refine((data) => data.newPassword === data.confirmNewPassword, {
  message: "New passwords don't match",
  path: ["confirmNewPassword"],
});

type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

export default function ChangePasswordPage() {
  const [isLoading, setIsLoading] = useState(false);
  const { changePassword } = useAuth();
  const toast = useToast();
  const { colorMode } = useColorMode();
  const router = useRouter();

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
  });

  const onSubmit = async (data: ChangePasswordFormData) => {
    setIsLoading(true);
    try {
      await changePassword(data.currentPassword, data.newPassword);
      toast({
        title: 'Password changed successfully',
        description: 'Your password has been updated',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
      reset();
      // Use router.push instead of window.location for client-side navigation
      router.push('/login');
    } catch (error) {
      toast({
        title: 'Password change failed',
        description: 'An error occurred while changing your password',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <ProtectedRoute>
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
                Change your password
              </Heading>
              <Text 
                color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                fontSize="md"
                lineHeight="1.5"
              >
                Keep your account secure by updating your password regularly
              </Text>
            </Box>

            {/* Right side - Change Password Form */}
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
                      Change Password
                    </Heading>
                    <Text 
                      color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                      fontSize="sm"
                    >
                      Enter your current password and choose a new one
                    </Text>
                  </Box>

                  <form onSubmit={handleSubmit(onSubmit)}>
                    <VStack spacing={4}>
                      <FormControl isInvalid={!!errors.currentPassword}>
                        <FormLabel 
                          fontWeight="medium" 
                          fontSize="sm"
                          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
                          mb={1}
                        >
                          Current Password
                        </FormLabel>
                        <InputGroup>
                          <InputLeftElement pointerEvents="none">
                            <FiLock color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.400'} />
                          </InputLeftElement>
                          <Input
                            type="password"
                            {...register('currentPassword')}
                            placeholder="Current Password"
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
                        <FormErrorMessage>{errors.currentPassword?.message}</FormErrorMessage>
                      </FormControl>

                      <FormControl isInvalid={!!errors.newPassword}>
                        <FormLabel 
                          fontWeight="medium" 
                          fontSize="sm"
                          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
                          mb={1}
                        >
                          New Password
                        </FormLabel>
                        <InputGroup>
                          <InputLeftElement pointerEvents="none">
                            <FiLock color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.400'} />
                          </InputLeftElement>
                          <Input
                            type="password"
                            {...register('newPassword')}
                            placeholder="New Password"
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
                        <FormErrorMessage>{errors.newPassword?.message}</FormErrorMessage>
                      </FormControl>

                      <FormControl isInvalid={!!errors.confirmNewPassword}>
                        <FormLabel 
                          fontWeight="medium" 
                          fontSize="sm"
                          color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.700'}
                          mb={1}
                        >
                          Confirm New Password
                        </FormLabel>
                        <InputGroup>
                          <InputLeftElement pointerEvents="none">
                            <FiLock color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.400'} />
                          </InputLeftElement>
                          <Input
                            type="password"
                            {...register('confirmNewPassword')}
                            placeholder="Confirm New Password"
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
                        <FormErrorMessage>{errors.confirmNewPassword?.message}</FormErrorMessage>
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
                        loadingText="Changing Password..."
                      >
                        Change Password
                      </Button>
                    </VStack>
                  </form>
                </VStack>
              </Box>
            </Box>
          </Flex>
        </Container>
      </Flex>
    </ProtectedRoute>
  );
} 
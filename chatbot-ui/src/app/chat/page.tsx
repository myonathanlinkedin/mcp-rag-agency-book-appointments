'use client';

import { useState, useRef, useEffect } from 'react';
import {
  Box,
  Container,
  VStack,
  HStack,
  Input,
  Button,
  Text,
  useToast,
  Flex,
  IconButton,
  Avatar,
  Heading,
  Divider,
  useColorModeValue,
  Badge,
  Image,
  useColorMode,
} from '@chakra-ui/react';
import { FiSend, FiLogOut, FiMessageSquare, FiClock } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import ReactMarkdown from 'react-markdown';
import { PrismLight as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import type { Components } from 'react-markdown';
import api from '@/lib/api';
import { ThemeSwitcher } from '@/components/ThemeSwitcher';

interface Message {
  id: string;
  content: string;
  role: 'user' | 'assistant';
  timestamp: Date;
}

export default function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { logout, user } = useAuth();
  const toast = useToast();
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  const { colorMode } = useColorMode();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const handleSend = async () => {
    if (!input.trim()) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      content: input,
      role: 'user',
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await api.post('/chat', { message: input });
      const assistantMessage: Message = {
        id: response.data.id,
        content: response.data.content,
        role: 'assistant',
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, assistantMessage]);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to send message',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  return (
    <ProtectedRoute>
      <Box 
        h="100vh" 
        bg={colorMode === 'dark' ? 'linkedin.dark.bg' : 'linkedin.light.bg'} 
        display="flex" 
        flexDirection="column"
      >
        {/* Header */}
        <Box
          as="header"
          bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
          borderBottom="1px"
          borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
          position="sticky"
          top={0}
          zIndex={10}
        >
          <Container maxW="container.xl" py={3}>
            <Flex justify="space-between" align="center">
              <HStack spacing={4}>
                <Image
                  src="/logo-placeholder.svg"
                  alt="Logo"
                  boxSize="32px"
                  borderRadius="md"
                />
                <Heading 
                  size="md" 
                  color="brand.600"
                  fontWeight="bold"
                  letterSpacing="tight"
                >
                  Agent Book
                </Heading>
                <Badge 
                  colorScheme="green" 
                  variant="subtle" 
                  px={2} 
                  py={1}
                  borderRadius="full"
                  fontSize="xs"
                >
                  Online
                </Badge>
              </HStack>
              <HStack spacing={4}>
                <Text 
                  fontSize="sm" 
                  color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.600'}
                  fontWeight="medium"
                >
                  {user?.email}
                </Text>
                <Avatar 
                  size="sm" 
                  name={user?.email} 
                  bg="brand.500"
                  color="white"
                />
                <ThemeSwitcher />
                <IconButton
                  aria-label="Logout"
                  icon={<FiLogOut />}
                  variant="ghost"
                  colorScheme="gray"
                  size="sm"
                  onClick={logout}
                  _hover={{
                    bg: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.100',
                  }}
                />
              </HStack>
            </Flex>
          </Container>
        </Box>

        {/* Main Chat Area */}
        <Container 
          maxW="container.xl" 
          flex="1" 
          display="flex" 
          flexDirection="column" 
          py={6}
          px={4}
        >
          <Box 
            flex="1" 
            display="flex" 
            flexDirection="column"
            bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
            borderRadius="md"
            border="1px"
            borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
            boxShadow="sm"
            overflow="hidden"
          >
            {/* Messages */}
            <Box 
              ref={messagesEndRef}
              flex="1"
              overflowY="auto"
              p={6}
              bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
              css={{
                '&::-webkit-scrollbar': {
                  width: '4px',
                },
                '&::-webkit-scrollbar-track': {
                  width: '6px',
                  background: 'transparent',
                },
                '&::-webkit-scrollbar-thumb': {
                  background: colorMode === 'dark' ? 'linkedin.dark.border' : 'gray.300',
                  borderRadius: '24px',
                  '&:hover': {
                    background: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.400',
                  },
                },
              }}
            >
              <VStack spacing={4} align="stretch">
                {messages.map((message) => (
                  <Box
                    key={message.id}
                    alignSelf={message.role === 'user' ? 'flex-end' : 'flex-start'}
                    maxW="70%"
                  >
                    <HStack
                      spacing={3}
                      align="flex-start"
                      bg={message.role === 'user' 
                        ? (colorMode === 'dark' ? 'linkedin.dark.message.user' : 'linkedin.light.message.user')
                        : (colorMode === 'dark' ? 'linkedin.dark.message.assistant' : 'linkedin.light.message.assistant')
                      }
                      color={colorMode === 'dark' ? 'white' : 'gray.800'}
                      p={4}
                      borderRadius="lg"
                      border="1px"
                      borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
                    >
                      <Avatar
                        size="sm"
                        name={message.role === 'user' ? user?.email : 'Assistant'}
                        bg={message.role === 'user' ? 'brand.500' : 'gray.500'}
                        color="white"
                      />
                      <Box flex={1}>
                        <ReactMarkdown
                          components={{
                            code: ({ className, children, ...props }) => {
                              const match = /language-(\w+)/.exec(className || '');
                              const language = match ? match[1] : '';
                              const isInline = !className || !match;
                              
                              if (isInline) {
                                return (
                                  <code 
                                    className={className} 
                                    {...props}
                                    style={{
                                      background: message.role === 'user' 
                                        ? (colorMode === 'dark' ? 'rgba(255, 255, 255, 0.1)' : 'rgba(255, 255, 255, 0.2)')
                                        : (colorMode === 'dark' ? 'linkedin.dark.bg' : 'gray.100'),
                                      padding: '0.2em 0.4em',
                                      borderRadius: '3px',
                                      fontSize: '0.9em',
                                      color: message.role === 'user' ? 'white' : (colorMode === 'dark' ? 'white' : 'gray.900'),
                                    }}
                                  >
                                    {children}
                                  </code>
                                );
                              }

                              return (
                                <Box 
                                  as="pre" 
                                  p={4} 
                                  borderRadius="md" 
                                  bg={message.role === 'user' ? 'rgba(0, 0, 0, 0.2)' : 'gray.800'} 
                                  overflowX="auto"
                                  fontSize="sm"
                                  color="white"
                                >
                                  <code className={className} {...props}>
                                    {children}
                                  </code>
                                </Box>
                              );
                            },
                            p: ({ children, ...props }) => (
                              <Text 
                                color={message.role === 'user' ? 'white' : (colorMode === 'dark' ? 'white' : 'gray.800')}
                                {...props}
                              >
                                {children}
                              </Text>
                            ),
                          } as Components}
                        >
                          {message.content}
                        </ReactMarkdown>
                        <HStack 
                          mt={2} 
                          spacing={2} 
                          color={colorMode === 'dark' ? 'gray.300' : 'gray.500'}
                          fontSize="xs"
                          opacity={1}
                          fontWeight={colorMode === 'dark' ? 'medium' : 'normal'}
                        >
                          <FiClock size={12} />
                          <Text>
                            {formatTime(message.timestamp)}
                          </Text>
                        </HStack>
                      </Box>
                    </HStack>
                  </Box>
                ))}
                <div ref={messagesEndRef} />
              </VStack>
            </Box>

            {/* Input Area */}
            <Box 
              p={4} 
              bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'} 
              borderTop="1px" 
              borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
            >
              <HStack>
                <Input
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="Type your message..."
                  disabled={isLoading}
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
                <IconButton
                  aria-label="Send message"
                  icon={<FiSend />}
                  colorScheme="brand"
                  size="md"
                  h="40px"
                  w="40px"
                  onClick={handleSend}
                  isLoading={isLoading}
                  _hover={{
                    bg: 'brand.700',
                  }}
                />
              </HStack>
              <Text 
                textAlign="center" 
                fontSize="xs" 
                color={colorMode === 'dark' ? 'linkedin.dark.text' : 'gray.500'}
                mt={4}
              >
                © 2025 Agent Book. All rights reserved. Developed by Mateus Yonathan.
              </Text>
            </Box>
          </Box>
        </Container>
      </Box>
    </ProtectedRoute>
  );
} 
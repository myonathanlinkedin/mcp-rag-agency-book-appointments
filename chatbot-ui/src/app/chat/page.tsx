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
import remarkGfm from 'remark-gfm';
import api from '@/lib/api';
import { ThemeSwitcher } from '@/components/ThemeSwitcher';
import { SettingsMenu } from '@/components/SettingsMenu';
import { TipsPopup, shouldShowTips } from '@/components/TipsPopup';

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
  const [showTips, setShowTips] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const { logout, user } = useAuth();
  const toast = useToast();
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  const { colorMode } = useColorMode();

  // Initialize tips popup
  useEffect(() => {
    // Check if we should show tips after a short delay to ensure smooth page load
    const timer = setTimeout(() => {
      if (typeof window !== 'undefined') {
        setShowTips(shouldShowTips());
      }
    }, 1000);

    return () => clearTimeout(timer);
  }, []);

  const scrollToBottom = () => {
    if (messagesContainerRef.current) {
      const scrollHeight = messagesContainerRef.current.scrollHeight;
      const height = messagesContainerRef.current.clientHeight;
      const maxScrollTop = scrollHeight - height;
      messagesContainerRef.current.scrollTop = maxScrollTop > 0 ? maxScrollTop : 0;
    }
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // Prevent body scrolling when messages container is focused
  useEffect(() => {
    const messagesContainer = messagesContainerRef.current;
    if (!messagesContainer) return;

    const preventScroll = (e: WheelEvent) => {
      const isAtTop = messagesContainer.scrollTop === 0;
      const isAtBottom = messagesContainer.scrollHeight - messagesContainer.clientHeight <= messagesContainer.scrollTop + 1;

      if ((isAtTop && e.deltaY < 0) || (isAtBottom && e.deltaY > 0)) {
        e.preventDefault();
      }
    };

    messagesContainer.addEventListener('wheel', preventScroll, { passive: false });
    return () => messagesContainer.removeEventListener('wheel', preventScroll);
  }, []);

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
    scrollToBottom();

    try {
      const response = await api.post('/api/Prompt/SendUserPrompt/SendUserPromptAsync', {
        prompt: input
      });
      
      const assistantMessage: Message = {
        id: Date.now().toString(),
        content: response.data,
        role: 'assistant',
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, assistantMessage]);
      scrollToBottom();
    } catch (err) {
      const error = err as { response?: { data?: { errors?: string[] } } };
      toast({
        title: 'Error',
        description: error.response?.data?.errors?.join(', ') || 'Failed to send message',
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
        <TipsPopup isOpen={showTips} onClose={() => setShowTips(false)} />
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
                  bg="brand.500"
                  border="1px"
                  borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
                  p={1}
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
                <SettingsMenu />
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
          position="relative"
          height="calc(100vh - 70px)" // Subtract header height
          overflow="hidden"
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
            position="relative"
          >
            {/* Messages */}
            <Box 
              ref={messagesContainerRef}
              flex="1"
              overflowY="auto"
              p={6}
              bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'}
              position="relative"
              css={{
                '&::-webkit-scrollbar': {
                  width: '4px',
                },
                '&::-webkit-scrollbar-track': {
                  width: '6px',
                  background: colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card',
                },
                '&::-webkit-scrollbar-thumb': {
                  background: colorMode === 'dark' ? 'linkedin.dark.border' : 'gray.300',
                  borderRadius: '24px',
                },
                '&::-webkit-scrollbar-thumb:hover': {
                  background: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.400',
                },
                scrollbarWidth: 'thin',
                scrollbarColor: `${colorMode === 'dark' ? 'linkedin.dark.border' : 'gray.300'} transparent`,
                // Prevent elastic scrolling on macOS
                overscrollBehavior: 'contain',
                // Smooth scrolling
                scrollBehavior: 'smooth',
              }}
            >
              <VStack spacing={6} align="stretch">
                {messages.map((message) => (
                  <Box
                    key={message.id}
                    alignSelf={message.role === 'user' ? 'flex-end' : 'flex-start'}
                    maxW="70%"
                    data-message-role={message.role}
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
                      width="100%"
                      boxShadow="sm"
                      _hover={{
                        borderColor: colorMode === 'dark' ? 'linkedin.dark.hover' : 'gray.400',
                      }}
                      transition="all 0.2s"
                    >
                      <Avatar
                        size="sm"
                        name={message.role === 'user' ? user?.email : 'Assistant'}
                        bg={message.role === 'user' ? 'brand.500' : 'gray.500'}
                        color="white"
                        flexShrink={0}
                      />
                      <Box 
                        flex="1" 
                        overflowWrap="break-word" 
                        wordBreak="break-word"
                        minW="0" // Ensure proper text wrapping
                      >
                        <ReactMarkdown
                          remarkPlugins={[remarkGfm]}
                          components={{
                            table: ({ children }) => (
                              <Box overflowX="auto" my={4}>
                                <Box
                                  as="table"
                                  width="100%"
                                  style={{
                                    borderCollapse: 'collapse',
                                    tableLayout: 'fixed',
                                  }}
                                >
                                  {children}
                                </Box>
                              </Box>
                            ),
                            thead: ({ children }) => (
                              <Box
                                as="thead"
                                bg={colorMode === 'dark' ? 'whiteAlpha.100' : 'gray.50'}
                              >
                                {children}
                              </Box>
                            ),
                            th: ({ children }) => (
                              <Box
                                as="th"
                                py={2}
                                px={4}
                                borderWidth="1px"
                                borderColor={colorMode === 'dark' ? 'whiteAlpha.300' : 'gray.200'}
                                textAlign="left"
                                fontWeight="semibold"
                                fontSize="sm"
                              >
                                {children}
                              </Box>
                            ),
                            tr: ({ children }) => (
                              <Box
                                as="tr"
                                _hover={{
                                  bg: colorMode === 'dark' ? 'whiteAlpha.50' : 'gray.50',
                                }}
                              >
                                {children}
                              </Box>
                            ),
                            td: ({ children }) => (
                              <Box
                                as="td"
                                py={2}
                                px={4}
                                borderWidth="1px"
                                borderColor={colorMode === 'dark' ? 'whiteAlpha.300' : 'gray.200'}
                                fontSize="sm"
                              >
                                {children}
                              </Box>
                            ),
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
                                        ? (colorMode === 'dark' ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)')
                                        : (colorMode === 'dark' ? 'linkedin.dark.bg' : 'gray.100'),
                                      padding: '0.2em 0.4em',
                                      borderRadius: '3px',
                                      fontSize: '0.9em',
                                      color: message.role === 'user' 
                                        ? (colorMode === 'dark' ? 'white' : 'white')
                                        : (colorMode === 'dark' ? 'white' : 'gray.900'),
                                      wordBreak: 'break-word',
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
                                  whiteSpace="pre-wrap"
                                  wordBreak="break-word"
                                >
                                  <code className={className} {...props}>
                                    {children}
                                  </code>
                                </Box>
                              );
                            },
                            p: ({ children, ...props }) => (
                              <Text 
                                color={
                                  message.role === 'user'
                                    ? (colorMode === 'dark' ? 'white' : 'white')
                                    : (colorMode === 'dark' ? 'white' : 'gray.800')
                                }
                                whiteSpace="pre-wrap"
                                wordBreak="break-word"
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
                <Box ref={messagesEndRef} />
              </VStack>
            </Box>

            {/* Input Area */}
            <Box 
              p={4} 
              bg={colorMode === 'dark' ? 'linkedin.dark.card' : 'linkedin.light.card'} 
              borderTop="1px" 
              borderColor={colorMode === 'dark' ? 'linkedin.dark.border' : 'linkedin.light.border'}
              position="sticky"
              bottom={0}
              width="100%"
              zIndex={1}
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
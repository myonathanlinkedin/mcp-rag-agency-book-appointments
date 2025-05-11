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
} from '@chakra-ui/react';
import { FiSend, FiLogOut } from 'react-icons/fi';
import { useAuth } from '@/contexts/AuthContext';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import ReactMarkdown from 'react-markdown';
import { PrismLight as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import type { Components } from 'react-markdown';
import api from '@/lib/api';

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

  return (
    <ProtectedRoute>
      <Container maxW="container.xl" h="100vh" p={0}>
        <Flex h="full" direction="column">
          {/* Header */}
          <Box
            p={4}
            borderBottom="1px"
            borderColor="gray.200"
            bg="white"
            shadow="sm"
          >
            <Flex justify="space-between" align="center">
              <Text fontSize="xl" fontWeight="bold">
                Agent Book
              </Text>
              <HStack>
                <Avatar size="sm" name={user?.email} />
                <IconButton
                  aria-label="Logout"
                  icon={<FiLogOut />}
                  variant="ghost"
                  onClick={logout}
                />
              </HStack>
            </Flex>
          </Box>

          {/* Chat Messages */}
          <Box
            flex={1}
            overflowY="auto"
            p={4}
            bg="gray.50"
            css={{
              '&::-webkit-scrollbar': {
                width: '4px',
              },
              '&::-webkit-scrollbar-track': {
                width: '6px',
              },
              '&::-webkit-scrollbar-thumb': {
                background: 'gray.300',
                borderRadius: '24px',
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
                  <Box
                    bg={message.role === 'user' ? 'blue.500' : 'white'}
                    color={message.role === 'user' ? 'white' : 'black'}
                    p={4}
                    borderRadius="lg"
                    boxShadow="sm"
                  >
                    <ReactMarkdown
                      components={{
                        code: ({ className, children, ...props }) => {
                          const match = /language-(\w+)/.exec(className || '');
                          const language = match ? match[1] : '';
                          const isInline = !className || !match;
                          
                          if (isInline) {
                            return (
                              <code className={className} {...props}>
                                {children}
                              </code>
                            );
                          }

                          return (
                            <Box as="pre" p={4} borderRadius="md" bg="gray.800" overflowX="auto">
                              <code className={className} {...props}>
                                {children}
                              </code>
                            </Box>
                          );
                        },
                      } as Components}
                    >
                      {message.content}
                    </ReactMarkdown>
                  </Box>
                  <Text fontSize="xs" color="gray.500" mt={1}>
                    {message.timestamp.toLocaleTimeString()}
                  </Text>
                </Box>
              ))}
              <div ref={messagesEndRef} />
            </VStack>
          </Box>

          {/* Input Area */}
          <Box p={4} bg="white" borderTop="1px" borderColor="gray.200">
            <HStack>
              <Input
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder="Type your message..."
                disabled={isLoading}
              />
              <IconButton
                aria-label="Send message"
                icon={<FiSend />}
                colorScheme="blue"
                onClick={handleSend}
                isLoading={isLoading}
              />
            </HStack>
          </Box>
        </Flex>
      </Container>
    </ProtectedRoute>
  );
} 
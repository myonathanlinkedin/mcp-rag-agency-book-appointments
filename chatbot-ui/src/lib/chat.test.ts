import { Chat, ChatMessage, ChatRole } from '@/lib/chat';

describe('Chat', () => {
  let chat: Chat;

  beforeEach(() => {
    chat = new Chat();
  });

  it('should create a chat instance', () => {
    expect(chat).toBeDefined();
  });

  it('should add a message', () => {
    const message: ChatMessage = {
      role: ChatRole.User,
      content: 'Hello, world!',
    };
    chat.addMessage(message);
    const messages = chat.getMessages();
    expect(messages).toHaveLength(1);
    expect(messages[0]).toEqual(message);
  });

  it('should clear messages', () => {
    chat.addMessage({ role: ChatRole.User, content: 'Hello' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'Hi' });
    chat.clearMessages();
    expect(chat.getMessages()).toHaveLength(0);
  });

  it('should get recent messages', () => {
    chat.addMessage({ role: ChatRole.User, content: 'Hello' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'Hi' });
    chat.addMessage({ role: ChatRole.User, content: 'How are you?' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'I am fine, thank you.' });

    const recentMessages = chat.getRecentMessages(2);
    expect(recentMessages).toHaveLength(2);
    expect(recentMessages[0].content).toBe('How are you?');
    expect(recentMessages[1].content).toBe('I am fine, thank you.');
  });

  it('should get messages by role', () => {
    chat.addMessage({ role: ChatRole.User, content: 'Hello' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'Hi' });
    chat.addMessage({ role: ChatRole.User, content: 'How are you?' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'I am fine, thank you.' });

    const userMessages = chat.getMessagesByRole(ChatRole.User);
    expect(userMessages).toHaveLength(2);
    expect(userMessages[0].content).toBe('Hello');
    expect(userMessages[1].content).toBe('How are you?');

    const assistantMessages = chat.getMessagesByRole(ChatRole.Assistant);
    expect(assistantMessages).toHaveLength(2);
    expect(assistantMessages[0].content).toBe('Hi');
    expect(assistantMessages[1].content).toBe('I am fine, thank you.');
  });

  it('should get last message', () => {
    expect(chat.getLastMessage()).toBeUndefined();
    chat.addMessage({ role: ChatRole.User, content: 'Hello' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'Hi' });
    expect(chat.getLastMessage()?.content).toBe('Hi');
  });

  it('should get last message by role', () => {
    expect(chat.getLastMessageByRole(ChatRole.User)).toBeUndefined();
    chat.addMessage({ role: ChatRole.User, content: 'Hello' });
    chat.addMessage({ role: ChatRole.Assistant, content: 'Hi' });
    chat.addMessage({ role: ChatRole.User, content: 'How are you?' });
    expect(chat.getLastMessageByRole(ChatRole.User)?.content).toBe('How are you?');
    expect(chat.getLastMessageByRole(ChatRole.Assistant)?.content).toBe('Hi');
  });
}); 
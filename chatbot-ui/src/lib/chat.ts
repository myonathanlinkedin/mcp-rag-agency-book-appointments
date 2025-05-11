export enum ChatRole {
  User = 'user',
  Assistant = 'assistant',
}

export interface ChatMessage {
  role: ChatRole;
  content: string;
}

export class Chat {
  private messages: ChatMessage[] = [];

  addMessage(message: ChatMessage) {
    this.messages.push(message);
  }

  getMessages(): ChatMessage[] {
    return [...this.messages];
  }

  clearMessages() {
    this.messages = [];
  }

  getRecentMessages(limit: number): ChatMessage[] {
    return this.messages.slice(-limit);
  }

  getMessagesByRole(role: ChatRole): ChatMessage[] {
    return this.messages.filter(message => message.role === role);
  }

  getLastMessage(): ChatMessage | undefined {
    return this.messages[this.messages.length - 1];
  }

  getLastMessageByRole(role: ChatRole): ChatMessage | undefined {
    for (let i = this.messages.length - 1; i >= 0; i--) {
      if (this.messages[i].role === role) {
        return this.messages[i];
      }
    }
    return undefined;
  }
} 
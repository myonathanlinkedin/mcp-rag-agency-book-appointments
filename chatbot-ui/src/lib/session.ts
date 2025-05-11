import { Logger } from './logger';

interface Session {
  id: string;
  userId: string;
  email: string;
  token: string;
  expiresAt: Date;
  lastActivity: Date;
}

export class SessionManager {
  private static instance: SessionManager;
  private logger: Logger;
  private session: Session | null = null;
  private readonly SESSION_KEY = 'app_session';
  private readonly SESSION_TIMEOUT = 30 * 60 * 1000; // 30 minutes
  private activityCheckInterval: NodeJS.Timeout | null = null;

  private constructor() {
    this.logger = new Logger('SessionManager');
    this.loadSession();
    this.startActivityCheck();
  }

  static getInstance(): SessionManager {
    if (!SessionManager.instance) {
      SessionManager.instance = new SessionManager();
    }
    return SessionManager.instance;
  }

  private loadSession() {
    try {
      const sessionStr = localStorage.getItem(this.SESSION_KEY);
      if (sessionStr) {
        const session = JSON.parse(sessionStr);
        // Convert string dates back to Date objects
        session.expiresAt = new Date(session.expiresAt);
        session.lastActivity = new Date(session.lastActivity);
        
        // Check if session is still valid
        if (session.expiresAt > new Date()) {
          this.session = session;
          this.logger.info('Session loaded', { userId: session.userId });
        } else {
          this.logger.info('Session expired', { userId: session.userId });
          this.clearSession();
        }
      }
    } catch (error) {
      this.logger.error('Failed to load session', error);
      this.clearSession();
    }
  }

  private saveSession(session: Session) {
    try {
      localStorage.setItem(this.SESSION_KEY, JSON.stringify(session));
      this.session = session;
      this.logger.info('Session saved', { userId: session.userId });
    } catch (error) {
      this.logger.error('Failed to save session', error);
    }
  }

  private startActivityCheck() {
    // Check session activity every minute
    this.activityCheckInterval = setInterval(() => {
      this.checkSessionActivity();
    }, 60 * 1000);
  }

  private checkSessionActivity() {
    if (!this.session) return;

    const now = new Date();
    const timeSinceLastActivity = now.getTime() - this.session.lastActivity.getTime();

    if (timeSinceLastActivity > this.SESSION_TIMEOUT) {
      this.logger.info('Session timed out due to inactivity', { 
        userId: this.session.userId,
        lastActivity: this.session.lastActivity
      });
      this.clearSession();
    }
  }

  updateActivity() {
    if (this.session) {
      this.session.lastActivity = new Date();
      this.saveSession(this.session);
    }
  }

  createSession(userId: string, email: string, token: string) {
    const session: Session = {
      id: crypto.randomUUID(),
      userId,
      email,
      token,
      expiresAt: new Date(Date.now() + this.SESSION_TIMEOUT),
      lastActivity: new Date(),
    };

    this.saveSession(session);
    return session;
  }

  getSession(): Session | null {
    return this.session;
  }

  getToken(): string | null {
    return this.session?.token || null;
  }

  clearSession() {
    this.session = null;
    localStorage.removeItem(this.SESSION_KEY);
    this.logger.info('Session cleared');
  }

  isAuthenticated(): boolean {
    return !!this.session && this.session.expiresAt > new Date();
  }

  // Cleanup
  destroy() {
    if (this.activityCheckInterval) {
      clearInterval(this.activityCheckInterval);
    }
  }
}

// Export a singleton instance
export const sessionManager = SessionManager.getInstance(); 
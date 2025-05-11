import { SessionManager } from './session';

describe('SessionManager', () => {
  let sessionManager: SessionManager;

  beforeEach(() => {
    sessionManager = SessionManager.getInstance();
    localStorage.clear();
  });

  afterEach(() => {
    sessionManager.destroy();
  });

  it('should create a singleton instance', () => {
    const instance1 = SessionManager.getInstance();
    const instance2 = SessionManager.getInstance();
    expect(instance1).toBe(instance2);
  });

  it('should create a session', () => {
    const session = sessionManager.createSession('user123', 'user@example.com', 'token123');
    expect(session).toBeDefined();
    expect(session.userId).toBe('user123');
    expect(session.email).toBe('user@example.com');
    expect(session.token).toBe('token123');
    expect(session.expiresAt).toBeInstanceOf(Date);
    expect(session.lastActivity).toBeInstanceOf(Date);
  });

  it('should save session to localStorage', () => {
    sessionManager.createSession('user123', 'user@example.com', 'token123');
    const sessionStr = localStorage.getItem('app_session');
    expect(sessionStr).toBeDefined();
    const session = JSON.parse(sessionStr!);
    expect(session.userId).toBe('user123');
  });

  it('should load session from localStorage', () => {
    const session = sessionManager.createSession('user123', 'user@example.com', 'token123');
    const newInstance = SessionManager.getInstance();
    const loadedSession = newInstance.getSession();
    expect(loadedSession).toBeDefined();
    expect(loadedSession!.userId).toBe(session.userId);
  });

  it('should clear session', () => {
    sessionManager.createSession('user123', 'user@example.com', 'token123');
    sessionManager.clearSession();
    expect(sessionManager.getSession()).toBeNull();
    expect(localStorage.getItem('app_session')).toBeNull();
  });

  it('should check if user is authenticated', () => {
    expect(sessionManager.isAuthenticated()).toBe(false);
    sessionManager.createSession('user123', 'user@example.com', 'token123');
    expect(sessionManager.isAuthenticated()).toBe(true);
    sessionManager.clearSession();
    expect(sessionManager.isAuthenticated()).toBe(false);
  });

  it('should get token', () => {
    expect(sessionManager.getToken()).toBeNull();
    sessionManager.createSession('user123', 'user@example.com', 'token123');
    expect(sessionManager.getToken()).toBe('token123');
  });

  it('should update last activity', () => {
    const session = sessionManager.createSession('user123', 'user@example.com', 'token123');
    const initialActivity = session.lastActivity;
    sessionManager.updateActivity();
    const updatedSession = sessionManager.getSession();
    expect(updatedSession!.lastActivity.getTime()).toBeGreaterThan(initialActivity.getTime());
  });

  it('should handle expired session', () => {
    const session = sessionManager.createSession('user123', 'user@example.com', 'token123');
    // Manually expire the session
    session.expiresAt = new Date(Date.now() - 1000);
    localStorage.setItem('app_session', JSON.stringify(session));
    
    // Create new instance to trigger session load
    const newInstance = SessionManager.getInstance();
    expect(newInstance.getSession()).toBeNull();
    expect(newInstance.isAuthenticated()).toBe(false);
  });
}); 
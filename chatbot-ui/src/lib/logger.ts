type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LogEntry {
  timestamp: string;
  level: LogLevel;
  message: string;
  context?: any;
}

export class Logger {
  private name: string;
  private logs: LogEntry[] = [];
  private readonly MAX_LOGS = 1000;

  constructor(name: string) {
    this.name = name;
  }

  private log(level: LogLevel, message: string, context?: any) {
    const entry: LogEntry = {
      timestamp: new Date().toISOString(),
      level,
      message,
      context,
    };

    // Add to in-memory logs
    this.logs.push(entry);
    if (this.logs.length > this.MAX_LOGS) {
      this.logs.shift(); // Remove oldest log
    }

    // Format log message
    const logMessage = `[${entry.timestamp}] [${level.toUpperCase()}] [${this.name}] ${message}`;

    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      switch (level) {
        case 'debug':
          console.debug(logMessage, context);
          break;
        case 'info':
          console.info(logMessage, context);
          break;
        case 'warn':
          console.warn(logMessage, context);
          break;
        case 'error':
          console.error(logMessage, context);
          break;
      }
    }

    // In production, you would send logs to a logging service
    if (process.env.NODE_ENV === 'production') {
      // Example: Send to logging service
      this.sendToLoggingService(entry);
    }
  }

  private async sendToLoggingService(entry: LogEntry) {
    try {
      // Example: Send to logging service
      // await fetch('https://your-logging-service.com/logs', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify(entry),
      // });
    } catch (error) {
      console.error('Failed to send log to logging service:', error);
    }
  }

  debug(message: string, context?: any) {
    this.log('debug', message, context);
  }

  info(message: string, context?: any) {
    this.log('info', message, context);
  }

  warn(message: string, context?: any) {
    this.log('warn', message, context);
  }

  error(message: string, context?: any) {
    this.log('error', message, context);
  }

  // Get recent logs
  getRecentLogs(level?: LogLevel, limit: number = 100): LogEntry[] {
    let filtered = this.logs;
    if (level) {
      filtered = filtered.filter(log => log.level === level);
    }
    return filtered.slice(-limit);
  }

  // Clear logs
  clearLogs() {
    this.logs = [];
  }
} 
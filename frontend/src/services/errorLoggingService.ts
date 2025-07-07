import { ApiError } from '../utils/apiErrorHandler';

interface ErrorLog {
  timestamp: string;
  message: string;
  stack?: string;
  correlationId?: string;
  type: string;
  severity: 'error' | 'warning' | 'info';
  context: {
    url: string;
    userAgent: string;
    userId?: string;
    sessionId: string;
  };
  metadata?: any;
}

class ErrorLoggingService {
  private sessionId: string;
  private userId?: string;
  private logQueue: ErrorLog[] = [];
  private flushTimer?: NodeJS.Timeout;
  private readonly MAX_QUEUE_SIZE = 10;
  private readonly FLUSH_INTERVAL = 30000; // 30 seconds

  constructor() {
    // Generate session ID
    this.sessionId = this.generateSessionId();
    
    // Start periodic flush
    this.startPeriodicFlush();
    
    // Add window error handler
    this.setupGlobalErrorHandlers();
  }

  private generateSessionId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private setupGlobalErrorHandlers() {
    // Handle unhandled promise rejections
    window.addEventListener('unhandledrejection', (event) => {
      this.logError('Unhandled promise rejection', event.reason, 'UnhandledRejection');
    });

    // Handle global errors
    const originalError = window.onerror;
    window.onerror = (message, source, lineno, colno, error) => {
      this.logError(
        typeof message === 'string' ? message : 'Unknown error',
        error,
        'GlobalError',
        {
          source,
          lineno,
          colno
        }
      );
      
      // Call original handler if exists
      if (originalError) {
        return originalError(message, source, lineno, colno, error);
      }
      return true;
    };
  }

  setUserId(userId: string | undefined) {
    this.userId = userId;
  }

  logError(message: string, error?: Error | ApiError | any, type: string = 'Error', metadata?: any) {
    const errorLog: ErrorLog = {
      timestamp: new Date().toISOString(),
      message,
      type,
      severity: 'error',
      context: {
        url: window.location.href,
        userAgent: navigator.userAgent,
        userId: this.userId,
        sessionId: this.sessionId
      },
      metadata
    };

    // Add error details
    if (error) {
      if (error instanceof ApiError) {
        errorLog.correlationId = error.correlationId;
        errorLog.metadata = {
          ...errorLog.metadata,
          status: error.status,
          apiErrorType: error.type,
          errors: error.errors
        };
      } else if (error instanceof Error) {
        errorLog.stack = error.stack;
      }
    }

    this.addToQueue(errorLog);
  }

  logWarning(message: string, metadata?: any) {
    const errorLog: ErrorLog = {
      timestamp: new Date().toISOString(),
      message,
      type: 'Warning',
      severity: 'warning',
      context: {
        url: window.location.href,
        userAgent: navigator.userAgent,
        userId: this.userId,
        sessionId: this.sessionId
      },
      metadata
    };

    this.addToQueue(errorLog);
  }

  logInfo(message: string, metadata?: any) {
    const errorLog: ErrorLog = {
      timestamp: new Date().toISOString(),
      message,
      type: 'Info',
      severity: 'info',
      context: {
        url: window.location.href,
        userAgent: navigator.userAgent,
        userId: this.userId,
        sessionId: this.sessionId
      },
      metadata
    };

    this.addToQueue(errorLog);
  }

  private addToQueue(log: ErrorLog) {
    this.logQueue.push(log);
    
    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      console.group(`[${log.severity.toUpperCase()}] ${log.type}`);
      console.log('Message:', log.message);
      console.log('Context:', log.context);
      if (log.metadata) {
        console.log('Metadata:', log.metadata);
      }
      if (log.stack) {
        console.log('Stack:', log.stack);
      }
      console.groupEnd();
    }

    // Flush if queue is full
    if (this.logQueue.length >= this.MAX_QUEUE_SIZE) {
      this.flush();
    }
  }

  private startPeriodicFlush() {
    this.flushTimer = setInterval(() => {
      if (this.logQueue.length > 0) {
        this.flush();
      }
    }, this.FLUSH_INTERVAL);
  }

  private async flush() {
    if (this.logQueue.length === 0) return;

    const logs = [...this.logQueue];
    this.logQueue = [];

    try {
      // In a real application, you would send these logs to your logging service
      // For now, we'll just store them in localStorage for debugging
      const existingLogs = this.getStoredLogs();
      const allLogs = [...existingLogs, ...logs];
      
      // Keep only the last 100 logs
      const recentLogs = allLogs.slice(-100);
      localStorage.setItem('errorLogs', JSON.stringify(recentLogs));

      // In production, you would send to your logging endpoint:
      // await fetch('/api/logs', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify({ logs })
      // });
    } catch (error) {
      console.error('Failed to flush error logs:', error);
      // Re-add logs to queue on failure
      this.logQueue = [...logs, ...this.logQueue];
    }
  }

  getStoredLogs(): ErrorLog[] {
    try {
      const stored = localStorage.getItem('errorLogs');
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  }

  clearStoredLogs() {
    localStorage.removeItem('errorLogs');
  }

  destroy() {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    this.flush();
  }
}

// Create singleton instance
export const errorLoggingService = new ErrorLoggingService();

// Export type for use in components
export type { ErrorLog };
import { Logger } from './logger';

describe('Logger', () => {
  let logger: Logger;

  beforeEach(() => {
    logger = new Logger('test');
    jest.spyOn(console, 'debug').mockImplementation(() => {});
    jest.spyOn(console, 'info').mockImplementation(() => {});
    jest.spyOn(console, 'warn').mockImplementation(() => {});
    jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('should create a logger instance', () => {
    expect(logger).toBeDefined();
  });

  it('should log debug messages', () => {
    logger.debug('Debug message', { foo: 'bar' });
    expect(console.debug).toHaveBeenCalledWith(
      expect.stringContaining('[DEBUG]'),
      { foo: 'bar' }
    );
  });

  it('should log info messages', () => {
    logger.info('Info message', { foo: 'bar' });
    expect(console.info).toHaveBeenCalledWith(
      expect.stringContaining('[INFO]'),
      { foo: 'bar' }
    );
  });

  it('should log warn messages', () => {
    logger.warn('Warn message', { foo: 'bar' });
    expect(console.warn).toHaveBeenCalledWith(
      expect.stringContaining('[WARN]'),
      { foo: 'bar' }
    );
  });

  it('should log error messages', () => {
    logger.error('Error message', { foo: 'bar' });
    expect(console.error).toHaveBeenCalledWith(
      expect.stringContaining('[ERROR]'),
      { foo: 'bar' }
    );
  });

  it('should store logs in memory', () => {
    logger.debug('Debug message');
    logger.info('Info message');
    logger.warn('Warn message');
    logger.error('Error message');

    const logs = logger.getRecentLogs();
    expect(logs).toHaveLength(4);
    expect(logs[0].level).toBe('debug');
    expect(logs[1].level).toBe('info');
    expect(logs[2].level).toBe('warn');
    expect(logs[3].level).toBe('error');
  });

  it('should limit the number of stored logs', () => {
    const MAX_LOGS = 1000;
    for (let i = 0; i < MAX_LOGS + 10; i++) {
      logger.debug(`Debug message ${i}`);
    }

    const logs = logger.getRecentLogs();
    expect(logs).toHaveLength(MAX_LOGS);
    expect(logs[logs.length - 1].message).toBe(`Debug message ${MAX_LOGS + 9}`);
  });

  it('should filter logs by level', () => {
    logger.debug('Debug message');
    logger.info('Info message');
    logger.warn('Warn message');
    logger.error('Error message');

    const infoLogs = logger.getRecentLogs('info');
    expect(infoLogs).toHaveLength(1);
    expect(infoLogs[0].level).toBe('info');

    const errorLogs = logger.getRecentLogs('error');
    expect(errorLogs).toHaveLength(1);
    expect(errorLogs[0].level).toBe('error');
  });

  it('should clear logs', () => {
    logger.debug('Debug message');
    logger.info('Info message');
    logger.clearLogs();

    const logs = logger.getRecentLogs();
    expect(logs).toHaveLength(0);
  });
}); 
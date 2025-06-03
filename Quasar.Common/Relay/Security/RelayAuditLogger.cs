using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Quasar.Common.Relay.Security
{
    /// <summary>
    /// Provides logging and auditing functionality for relay connections.
    /// Records security events, connection attempts, and rate limiting incidents.
    /// </summary>
    public class RelayAuditLogger
    {
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly ReaderWriterLockSlim _logLock = new ReaderWriterLockSlim();
        private readonly int _maxLogSizeBytes;
        private readonly int _maxLogFiles;
        
        /// <summary>
        /// Occurs when a new audit entry is logged.
        /// </summary>
        public event EventHandler<string> AuditEntryLogged;
        
        /// <summary>
        /// Creates a new instance of the RelayAuditLogger class.
        /// </summary>
        /// <param name="logDirectory">Directory to store log files. If null, uses the default application data directory.</param>
        /// <param name="maxLogSizeBytes">Maximum size of each log file in bytes (default: 5MB).</param>
        /// <param name="maxLogFiles">Maximum number of log files to keep (default: 5).</param>
        public RelayAuditLogger(string logDirectory = null, int maxLogSizeBytes = 5 * 1024 * 1024, int maxLogFiles = 5)
        {
            _maxLogSizeBytes = maxLogSizeBytes;
            _maxLogFiles = maxLogFiles;
            
            // Set up log directory
            _logDirectory = logDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Quasar", "Logs", "RelayAudit");
                
            // Ensure log directory exists
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            
            // Set up log file path
            _logFilePath = Path.Combine(_logDirectory, "relay_audit.log");
            
            // Log initialization
            LogAuditEvent("RelayAuditLogger initialized", AuditEventType.Info);
        }
        
        /// <summary>
        /// Logs an audit event to the log file.
        /// </summary>
        /// <param name="message">The audit message to log.</param>
        /// <param name="eventType">The type of audit event.</param>
        /// <param name="deviceId">Optional device ID associated with the event.</param>
        public void LogAuditEvent(string message, AuditEventType eventType, string deviceId = null)
        {
            if (string.IsNullOrEmpty(message))
                return;
                
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string deviceInfo = string.IsNullOrEmpty(deviceId) ? "" : $"[Device: {deviceId}] ";
            string logEntry = $"[{timestamp}] [{eventType}] {deviceInfo}{message}";
            
            // Write to log file
            WriteToLogFile(logEntry);
            
            // Raise event
            AuditEntryLogged?.Invoke(this, logEntry);
        }
        
        /// <summary>
        /// Logs a connection attempt to the log file.
        /// </summary>
        /// <param name="sourceDeviceId">The source device ID.</param>
        /// <param name="targetDeviceId">The target device ID.</param>
        /// <param name="successful">Whether the connection attempt was successful.</param>
        public void LogConnectionAttempt(string sourceDeviceId, string targetDeviceId, bool successful)
        {
            string status = successful ? "successful" : "failed";
            LogAuditEvent(
                $"Connection attempt from {sourceDeviceId} to {targetDeviceId} {status}",
                successful ? AuditEventType.ConnectionSuccess : AuditEventType.ConnectionFailure,
                sourceDeviceId);
        }
        
        /// <summary>
        /// Logs a rate limit event to the log file.
        /// </summary>
        /// <param name="deviceId">The device ID that exceeded the rate limit.</param>
        /// <param name="requestCount">The number of requests made.</param>
        /// <param name="timeWindowSeconds">The time window in seconds.</param>
        public void LogRateLimitExceeded(string deviceId, int requestCount, int timeWindowSeconds)
        {
            LogAuditEvent(
                $"Rate limit exceeded: {requestCount} requests in {timeWindowSeconds} seconds",
                AuditEventType.RateLimitExceeded,
                deviceId);
        }
        
        /// <summary>
        /// Gets the most recent audit events from the log file.
        /// </summary>
        /// <param name="count">The maximum number of events to retrieve.</param>
        /// <returns>A list of audit events.</returns>
        public List<string> GetRecentAuditEvents(int count)
        {
            _logLock.EnterReadLock();
            try
            {
                if (!File.Exists(_logFilePath))
                    return new List<string>();
                    
                return File.ReadLines(_logFilePath)
                    .Reverse()
                    .Take(count)
                    .Reverse()
                    .ToList();
            }
            finally
            {
                _logLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// Writes a log entry to the log file, with log rotation if needed.
        /// </summary>
        /// <param name="logEntry">The log entry to write.</param>
        private void WriteToLogFile(string logEntry)
        {
            _logLock.EnterWriteLock();
            try
            {
                // Check if log file exists and needs rotation
                if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > _maxLogSizeBytes)
                {
                    RotateLogs();
                }
                
                // Append to log file
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            finally
            {
                _logLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Rotates log files, keeping only the maximum number of log files.
        /// </summary>
        private void RotateLogs()
        {
            // Shift existing log files
            for (int i = _maxLogFiles - 1; i > 0; i--)
            {
                string source = i == 1 
                    ? _logFilePath 
                    : Path.Combine(_logDirectory, $"relay_audit.{i-1}.log");
                    
                string destination = Path.Combine(_logDirectory, $"relay_audit.{i}.log");
                
                if (File.Exists(source))
                {
                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }
                    
                    File.Move(source, destination);
                }
            }
            
            // Create new empty log file
            File.Create(_logFilePath).Close();
        }
    }
    
    /// <summary>
    /// Types of audit events.
    /// </summary>
    public enum AuditEventType
    {
        /// <summary>
        /// Informational event.
        /// </summary>
        Info,
        
        /// <summary>
        /// Warning event.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error event.
        /// </summary>
        Error,
        
        /// <summary>
        /// Security event.
        /// </summary>
        Security,
        
        /// <summary>
        /// Rate limit exceeded event.
        /// </summary>
        RateLimitExceeded,
        
        /// <summary>
        /// Successful connection event.
        /// </summary>
        ConnectionSuccess,
        
        /// <summary>
        /// Failed connection event.
        /// </summary>
        ConnectionFailure
    }
}

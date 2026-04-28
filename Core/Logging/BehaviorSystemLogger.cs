using UnityEngine;

namespace BehaviorSystem.Core.Logging
{
    /// <summary>
    /// Centralized logger for the Behavior System.
    /// Provides Log, LogWarning, and LogError methods with optional filtering.
    /// 
    /// Configure logging via BehaviorSystemLoggerSettings ScriptableObject.
    /// Create one via: Assets > Create > Behavior System > Logger Settings
    /// 
    /// LogError always surfaces to Unity's standard logging for visibility.
    /// </summary>
    public static class BehaviorSystemLogger
    {
        private const string LOG_PREFIX = "[BehaviorSystem]";
        private static BehaviorSystemLoggerSettings _settings;

        /// <summary>
        /// Gets the logger settings. Loads from Resources if not already cached.
        /// </summary>
        private static BehaviorSystemLoggerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Resources.Load<BehaviorSystemLoggerSettings>("BehaviorSystemLoggerSettings");
                    
                    if (_settings == null)
                    {
                        Debug.LogWarning(
                            $"{LOG_PREFIX} No BehaviorSystemLoggerSettings found in Resources folder. " +
                            "Create one via: Assets > Create > Behavior System > Logger Settings. " +
                            "Logging is disabled by default.");
                    }
                }
                return _settings;
            }
        }

        /// <summary>
        /// Logs a message if logging is enabled in settings.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">Optional Unity object for context (will be highlighted in console)</param>
        public static void Log(string message, Object context = null)
        {
            if (Settings != null && Settings.EnableLogging)
            {
                Debug.Log($"{LOG_PREFIX} {message}", context);
            }
        }

        /// <summary>
        /// Logs a warning if logging is enabled in settings.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="context">Optional Unity object for context (will be highlighted in console)</param>
        public static void LogWarning(string message, Object context = null)
        {
            if (Settings != null && Settings.EnableLogging)
            {
                Debug.LogWarning($"{LOG_PREFIX} {message}", context);
            }
        }

        /// <summary>
        /// Logs an error. Always surfaces to Unity's standard logging regardless of settings.
        /// This ensures critical errors are never missed.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="context">Optional Unity object for context (will be highlighted in console)</param>
        public static void LogError(string message, Object context = null)
        {
            // Always log errors to Unity's standard logging
            Debug.LogError($"{LOG_PREFIX} {message}", context);
        }
    }
}

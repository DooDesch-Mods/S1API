using MelonLoader;

namespace S1API.Logging
{
    /// <summary>
    /// Centralized logging wrapper for MelonLoader.
    /// </summary>
    public class Log
    {
        private readonly MelonLogger.Instance _loggerInstance;

        /// <summary>
        /// Creates a logger with the supplied source name.
        /// </summary>
        /// <param name="sourceName">The source name to use for logging.</param>
        public Log(string sourceName)
        {
            _loggerInstance = new MelonLogger.Instance(sourceName);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public void Msg(string message)
        {
            _loggerInstance.Msg(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public void Warning(string message)
        {
            _loggerInstance.Warning(message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public void Error(string message)
        {
            _loggerInstance.Error(message);
        }

        /// <summary>
        /// Logs a fatal error message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public void BigError(string message)
        {
            _loggerInstance.BigError(message);
        }
    }
}

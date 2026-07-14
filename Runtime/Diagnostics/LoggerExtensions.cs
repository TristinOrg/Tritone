using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Provides concise severity-specific logging operations.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Writes a trace event.
        /// </summary>
        /// <param name="logger">The logger receiving the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        public static void Trace(this ILogger logger, string category, string message)
        {
            logger.Log(ELogLevel.Trace, category, message);
        }

        /// <summary>
        /// Writes a debug event.
        /// </summary>
        /// <param name="logger">The logger receiving the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        public static void Debug(this ILogger logger, string category, string message)
        {
            logger.Log(ELogLevel.Debug, category, message);
        }

        /// <summary>
        /// Writes an informational event.
        /// </summary>
        /// <param name="logger">The logger receiving the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        public static void Info(this ILogger logger, string category, string message)
        {
            logger.Log(ELogLevel.Info, category, message);
        }

        /// <summary>
        /// Writes a warning event.
        /// </summary>
        /// <param name="logger">The logger receiving the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        public static void Warning(this ILogger logger, string category, string message)
        {
            logger.Log(ELogLevel.Warning, category, message);
        }

        /// <summary>
        /// Writes an error event.
        /// </summary>
        /// <param name="logger">The logger receiving the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        public static void Error(this ILogger logger, string category, string message, Exception exception = null)
        {
            logger.Log(ELogLevel.Error, category, message, exception);
        }

        /// <summary>
        /// Writes a fatal event.
        /// </summary>
        /// <param name="logger">The logger receiving the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        public static void Fatal(this ILogger logger, string category, string message, Exception exception = null)
        {
            logger.Log(ELogLevel.Fatal, category, message, exception);
        }
    }
}

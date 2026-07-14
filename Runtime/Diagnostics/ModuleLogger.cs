using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Provides category-bound and level-filtered logging for one module.
    /// </summary>
    public sealed class ModuleLogger : IModuleLogger
    {
        /// <summary>
        /// Stores the application-wide logger used to dispatch accepted events.
        /// </summary>
        private readonly ILogger mLogger;

        /// <summary>
        /// Stores the module category automatically attached to every event.
        /// </summary>
        private readonly string mCategory;

        /// <summary>
        /// Stores the minimum severity accepted by this module.
        /// </summary>
        private readonly ELogLevel mMinimumLevel;

        /// <summary>
        /// Initializes a logger bound to one module category.
        /// </summary>
        /// <param name="logger">The application-wide logger.</param>
        /// <param name="category">The module category attached to every event.</param>
        /// <param name="minimumLevel">The minimum severity accepted by this module.</param>
        public ModuleLogger(ILogger logger, string category, ELogLevel minimumLevel)
        {
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("A module log category is required.", nameof(category));

            mCategory     = category;
            mMinimumLevel = minimumLevel;
        }

        /// <summary>
        /// Determines whether the supplied severity is enabled by both module and global filters.
        /// </summary>
        /// <param name="level">The severity to test.</param>
        /// <returns>True when an event at this severity will be written; otherwise, false.</returns>
        public bool IsEnabled(ELogLevel level)
        {
            return mMinimumLevel != ELogLevel.Off && level >= mMinimumLevel && mLogger.IsEnabled(level);
        }

        /// <summary>
        /// Writes a trace event for this module.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        public void Trace(string message)
        {
            Log(ELogLevel.Trace, message);
        }

        /// <summary>
        /// Writes a debug event for this module.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        public void Debug(string message)
        {
            Log(ELogLevel.Debug, message);
        }

        /// <summary>
        /// Writes an informational event for this module.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        public void Info(string message)
        {
            Log(ELogLevel.Info, message);
        }

        /// <summary>
        /// Writes a warning event for this module.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        public void Warning(string message)
        {
            Log(ELogLevel.Warning, message);
        }

        /// <summary>
        /// Writes an error event for this module.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        public void Error(string message, Exception exception = null)
        {
            Log(ELogLevel.Error, message, exception);
        }

        /// <summary>
        /// Writes a fatal event for this module.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        public void Fatal(string message, Exception exception = null)
        {
            Log(ELogLevel.Fatal, message, exception);
        }

        /// <summary>
        /// Applies module filtering and forwards one event to the application logger.
        /// </summary>
        /// <param name="level">The severity of the event.</param>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        private void Log(ELogLevel level, string message, Exception exception = null)
        {
            if (!IsEnabled(level))
                return;

            mLogger.Log(level, mCategory, message, exception);
        }
    }
}

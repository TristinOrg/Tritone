using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Defines category-bound diagnostic logging available to one module.
    /// </summary>
    public interface IModuleLogger
    {
        /// <summary>
        /// Determines whether the supplied severity is enabled.
        /// </summary>
        /// <param name="level">The severity to test.</param>
        /// <returns>True when an event at this severity will be written; otherwise, false.</returns>
        bool IsEnabled(ELogLevel level);

        /// <summary>
        /// Writes a trace event.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        void Trace(string message);

        /// <summary>
        /// Writes a debug event.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        void Debug(string message);

        /// <summary>
        /// Writes an informational event.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        void Info(string message);

        /// <summary>
        /// Writes a warning event.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        void Warning(string message);

        /// <summary>
        /// Writes an error event.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        void Error(string message, Exception exception = null);

        /// <summary>
        /// Writes a fatal event.
        /// </summary>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        void Fatal(string message, Exception exception = null);
    }
}

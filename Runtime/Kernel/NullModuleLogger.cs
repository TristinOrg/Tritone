using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides allocation-free no-op logging when diagnostics are not configured.
    /// </summary>
    internal sealed class NullModuleLogger : IModuleLogger
    {
        /// <summary>
        /// Stores the shared no-op logger instance.
        /// </summary>
        internal static readonly NullModuleLogger Instance = new NullModuleLogger();

        /// <summary>
        /// Prevents external construction of the shared no-op logger.
        /// </summary>
        private NullModuleLogger() { }

        /// <summary>
        /// Reports that no severity is enabled.
        /// </summary>
        /// <param name="level">The severity to test.</param>
        /// <returns>Always false.</returns>
        public bool IsEnabled(ELogLevel level) => false;

        /// <summary>
        /// Ignores a trace event.
        /// </summary>
        /// <param name="message">The ignored event message.</param>
        public void Trace(string message) { }

        /// <summary>
        /// Ignores a debug event.
        /// </summary>
        /// <param name="message">The ignored event message.</param>
        public void Debug(string message) { }

        /// <summary>
        /// Ignores an informational event.
        /// </summary>
        /// <param name="message">The ignored event message.</param>
        public void Info(string message) { }

        /// <summary>
        /// Ignores a warning event.
        /// </summary>
        /// <param name="message">The ignored event message.</param>
        public void Warning(string message) { }

        /// <summary>
        /// Ignores an error event.
        /// </summary>
        /// <param name="message">The ignored event message.</param>
        /// <param name="exception">The ignored exception.</param>
        public void Error(string message, Exception exception = null) { }

        /// <summary>
        /// Ignores a fatal event.
        /// </summary>
        /// <param name="message">The ignored event message.</param>
        /// <param name="exception">The ignored exception.</param>
        public void Fatal(string message, Exception exception = null) { }
    }
}

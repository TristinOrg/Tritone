using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Defines the application-wide diagnostic logging service.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Determines whether the supplied severity is currently enabled.
        /// </summary>
        /// <param name="level">The severity to test.</param>
        /// <returns>True when events at this severity will be written; otherwise, false.</returns>
        bool IsEnabled(ELogLevel level);

        /// <summary>
        /// Writes one diagnostic log event.
        /// </summary>
        /// <param name="level">The severity of the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        void Log(ELogLevel level, string category, string message, Exception exception = null);
    }
}

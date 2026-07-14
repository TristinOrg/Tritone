using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Contains immutable data describing one diagnostic log event.
    /// </summary>
    public readonly struct LogEvent
    {
        /// <summary>
        /// Initializes one diagnostic log event.
        /// </summary>
        /// <param name="timestampUtc">The UTC time at which the event was created.</param>
        /// <param name="level">The severity of the event.</param>
        /// <param name="category">The subsystem or feature that created the event.</param>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        public LogEvent(DateTime timestampUtc,
                        ELogLevel level,
                        string category,
                        string message,
                        Exception exception)
        {
            TimestampUtc = timestampUtc;
            Level        = level;
            Category     = category;
            Message      = message;
            Exception    = exception;
        }

        /// <summary>
        /// Gets the UTC time at which the event was created.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Gets the severity of the event.
        /// </summary>
        public ELogLevel Level { get; }

        /// <summary>
        /// Gets the subsystem or feature that created the event.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the human-readable event message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the optional exception associated with the event.
        /// </summary>
        public Exception Exception { get; }
    }
}

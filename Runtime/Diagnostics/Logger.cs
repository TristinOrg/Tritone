using System;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Filters diagnostic events and dispatches them to immutable sink collections.
    /// </summary>
    public sealed class Logger : ILogger, IDisposable
    {
        /// <summary>
        /// Synchronizes sink writes and logger disposal across threads.
        /// </summary>
        private readonly object mSyncRoot = new();

        /// <summary>
        /// Stores the immutable log destinations.
        /// </summary>
        private readonly ILogSink[] mSinks;

        /// <summary>
        /// Stores the minimum severity accepted by this logger.
        /// </summary>
        private readonly ELogLevel mMinimumLevel;

        /// <summary>
        /// Indicates whether this logger has released its sinks.
        /// </summary>
        private bool mDisposed;

        /// <summary>
        /// Initializes a logger with a minimum severity and one or more sinks.
        /// </summary>
        /// <param name="minimumLevel">The minimum severity accepted by the logger.</param>
        /// <param name="sinks">The destinations that receive accepted events.</param>
        public Logger(ELogLevel minimumLevel, params ILogSink[] sinks)
        {
            if (sinks == null)
                throw new ArgumentNullException(nameof(sinks));

            mMinimumLevel = minimumLevel;
            mSinks        = new ILogSink[sinks.Length];
            for (int i = 0, cnt = sinks.Length; i < cnt; i++)
            {
                if (sinks[i] == null)
                    throw new ArgumentException("A log sink cannot be null.", nameof(sinks));
                mSinks[i] = sinks[i];
            }
        }

        /// <summary>
        /// Determines whether the supplied severity is currently enabled.
        /// </summary>
        /// <param name="level">The severity to test.</param>
        /// <returns>True when events at this severity will be written; otherwise, false.</returns>
        public bool IsEnabled(ELogLevel level)
        {
            return !mDisposed && mMinimumLevel != ELogLevel.Off && level >= mMinimumLevel && level < ELogLevel.Off;
        }

        /// <summary>
        /// Writes one diagnostic event to every configured sink.
        /// </summary>
        /// <param name="level">The severity of the event.</param>
        /// <param name="category">The subsystem or feature creating the event.</param>
        /// <param name="message">The human-readable event message.</param>
        /// <param name="exception">The optional exception associated with the event.</param>
        public void Log(ELogLevel level, string category, string message, Exception exception = null)
        {
            if (!IsEnabled(level))
                return;
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("A log category is required.", nameof(category));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            LogEvent logEvent = new(DateTime.UtcNow, level, category, message, exception);
            lock (mSyncRoot)
            {
                if (mDisposed)
                    return;
                for (int i = 0, cnt = mSinks.Length; i < cnt; i++)
                    mSinks[i].Write(in logEvent);
            }
        }

        /// <summary>
        /// Disposes every sink owned by this logger.
        /// </summary>
        public void Dispose()
        {
            lock (mSyncRoot)
            {
                if (mDisposed)
                    return;

                for (var i = mSinks.Length - 1; i >= 0; i--)
                {
                    if (mSinks[i] is IDisposable disposable)
                        disposable.Dispose();
                }
                mDisposed = true;
            }
        }
    }
}

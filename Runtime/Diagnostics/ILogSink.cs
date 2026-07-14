namespace Tritone.Diagnostics
{
    /// <summary>
    /// Defines a destination that receives diagnostic log events.
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Writes one diagnostic event to the destination.
        /// </summary>
        /// <param name="logEvent">The immutable event to write.</param>
        void Write(in LogEvent logEvent);
    }
}

using Tritone.Diagnostics;
using Tritone.Kernel;
using UnityEngine;

namespace Tritone.Unity
{
    /// <summary>
    /// Writes Tritone diagnostic events to the Unity Console.
    /// </summary>
    public sealed class UnityLogSink : ILogSink
    {
        /// <summary>
        /// Writes one diagnostic event using the corresponding Unity log severity.
        /// </summary>
        /// <param name="logEvent">The immutable event to write.</param>
        public void Write(in LogEvent logEvent)
        {
            var message = $"[{logEvent.TimestampUtc:HH:mm:ss.fff}] [{logEvent.Category}] {logEvent.Message}";
            switch (logEvent.Level)
            {
                case ELogLevel.Trace:
                case ELogLevel.Debug:
                case ELogLevel.Info:
                    Debug.Log(message);
                    break;
                case ELogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case ELogLevel.Error:
                case ELogLevel.Fatal:
                    Debug.LogError(message);
                    break;
            }

            if (logEvent.Exception != null)
                Debug.LogException(logEvent.Exception);
        }
    }
}

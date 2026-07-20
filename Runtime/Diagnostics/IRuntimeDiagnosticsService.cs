namespace Tritone.Diagnostics
{
    /// <summary>
    /// Exposes sampled application state and recent log events.
    /// </summary>
    public interface IRuntimeDiagnosticsService
    {
        /// <summary>
        /// Gets the latest immutable runtime snapshot.
        /// </summary>
        RuntimeDiagnosticSnapshot Snapshot { get; }

        /// <summary>
        /// Gets the fixed-capacity recent log buffer.
        /// </summary>
        RuntimeLogBufferSink Logs { get; }
    }
}

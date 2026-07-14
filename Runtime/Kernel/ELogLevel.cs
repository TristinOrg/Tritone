namespace Tritone.Kernel
{
    /// <summary>
    /// Defines the severity of a diagnostic log event.
    /// </summary>
    public enum ELogLevel
    {
        /// <summary>
        /// Contains highly detailed information used to trace execution flow.
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Contains development information used to diagnose behavior.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Contains normal information about application operation.
        /// </summary>
        Info = 2,

        /// <summary>
        /// Describes an unexpected condition from which the application can recover.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Describes an operation failure that requires investigation.
        /// </summary>
        Error = 4,

        /// <summary>
        /// Describes a critical failure that prevents normal application operation.
        /// </summary>
        Fatal = 5,

        /// <summary>
        /// Disables all log output.
        /// </summary>
        Off = 6
    }
}

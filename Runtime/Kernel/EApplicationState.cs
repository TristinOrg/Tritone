namespace Tritone.Kernel
{
    /// <summary>
    /// Defines every valid lifecycle state of a Tritone application.
    /// </summary>
    public enum EApplicationState
    {
        /// <summary>
        /// The application has been built but has not started.
        /// </summary>
        Created,

        /// <summary>
        /// The application is configuring and starting its modules.
        /// </summary>
        Starting,

        /// <summary>
        /// The application is running and accepts frame updates.
        /// </summary>
        Running,

        /// <summary>
        /// The application is stopping its modules.
        /// </summary>
        Stopping,

        /// <summary>
        /// The application has stopped and cannot be restarted.
        /// </summary>
        Stopped,

        /// <summary>
        /// The application failed during startup.
        /// </summary>
        Faulted
    }
}

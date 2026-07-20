namespace Tritone.Kernel
{
    /// <summary>
    /// Defines a feature module managed by the application lifecycle.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Registers services and resolves dependencies before any module starts.
        /// </summary>
        /// <param name="context">The independently owned capabilities and shared services available to this module.</param>
        void Configure(ModuleContext context);

        /// <summary>
        /// Starts the module after every module has completed configuration.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the module and releases all resources owned by it.
        /// </summary>
        void Stop();
    }
}

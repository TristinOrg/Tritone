namespace Tritone.Kernel
{
    /// <summary>
    /// Provides the standard lifecycle and automatic logging used by most modules.
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        /// <summary>
        /// Gets the minimum severity accepted by this module.
        /// </summary>
        protected virtual ELogLevel LogLevel => ELogLevel.Info;

        /// <summary>
        /// Gets the logger automatically bound to the concrete module type.
        /// </summary>
        protected IModuleLogger Logger { get; private set; } = NullModuleLogger.Instance;

        /// <summary>
        /// Creates the module logger and invokes module-specific configuration.
        /// </summary>
        /// <param name="context">The immutable application infrastructure available to this module.</param>
        public void Configure(ModuleContext context)
        {
            Logger = context.LoggerFactory.Create(GetType(), LogLevel);
            try
            {
                OnConfigure(context.Services);
            }
            catch
            {
                Logger = NullModuleLogger.Instance;
                throw;
            }
        }

        /// <summary>
        /// Invokes module-specific startup.
        /// </summary>
        public void Start()
        {
            OnStart();
        }

        /// <summary>
        /// Invokes module-specific shutdown and releases the module logger reference.
        /// </summary>
        public void Stop()
        {
            try
            {
                OnStop();
            }
            finally
            {
                Logger = NullModuleLogger.Instance;
            }
        }

        /// <summary>
        /// Configures services and dependencies required by the concrete module.
        /// </summary>
        /// <param name="services">The application-scoped service registry.</param>
        protected virtual void OnConfigure(IServiceRegistry services) { }

        /// <summary>
        /// Starts the concrete module.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Stops the concrete module.
        /// </summary>
        protected virtual void OnStop() { }
    }
}

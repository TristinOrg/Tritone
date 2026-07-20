using System;

namespace Tritone.Kernel
{
    /// <summary>
    /// Composes application services, module capabilities, and module-owned resources.
    /// </summary>
    public sealed class ModuleContext
    {
        /// <summary>
        // Stores the logger created for this concrete module.
        private IModuleLogger mLogger = NullModuleLogger.Instance;

        // Lazily stores module capabilities so unused systems allocate nothing.
        private TimerCapability mTimers;
        private EventCapability mEvents;
        private UICapability mUI;
        private SceneCapability mScenes;
        private AudioCapability mAudio;
        private SaveCapability mSaves;
        private SettingsCapability mSettings;
        private LocalizationCapability mLocalization;
        private PoolCapability mPools;
        private AssetCapability mAssets;
        private TableCapability mTables;
        private ContentCapability mContent;
        private InputCapability mInput;
        private NetworkCapability mNetwork;
        private ModelCapability mModels;

        /// <summary>
        /// Initializes one independently owned module context.
        /// </summary>
        /// <param name="services">The application-scoped service registry.</param>
        /// <param name="loggerFactory">The factory that creates category-bound module loggers.</param>
        internal ModuleContext(IServiceRegistry services, IModuleLoggerFactory loggerFactory)
        {
            Services      = services ?? throw new ArgumentNullException(nameof(services));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Scope         = new ModuleScope();
        }

        /// <summary>
        /// Gets the application-scoped service registry.
        /// </summary>
        public IServiceRegistry Services { get; }

        /// <summary>
        /// Gets the factory that creates category-bound module loggers.
        /// </summary>
        public IModuleLoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the generic lifetime owner shared by this module's capabilities.
        /// </summary>
        public ModuleScope Scope { get; }

        /// <summary>
        /// Gets the logger bound to the concrete module type.
        /// </summary>
        public IModuleLogger Logger => mLogger;

        /// <summary>
        /// Gets module-owned timer operations.
        /// </summary>
        public TimerCapability Timers => mTimers ??= new TimerCapability(this);

        /// <summary>
        /// Gets module-owned event binding operations.
        /// </summary>
        public EventCapability Events => mEvents ??= new EventCapability(this);

        /// <summary>
        /// Gets module-owned UI operations.
        /// </summary>
        public UICapability UI => mUI ??= new UICapability(this);

        /// <summary>
        /// Gets scene and scene-module switching operations.
        /// </summary>
        public SceneCapability Scenes => mScenes ??= new SceneCapability(this);

        /// <summary>
        /// Gets application audio operations.
        /// </summary>
        public AudioCapability Audio => mAudio ??= new AudioCapability(this);

        /// <summary>
        /// Gets local save operations.
        /// </summary>
        public SaveCapability Saves => mSaves ??= new SaveCapability(this);

        /// <summary>
        /// Gets typed application settings.
        /// </summary>
        public SettingsCapability Settings => mSettings ??= new SettingsCapability(this);

        /// <summary>
        /// Gets localization operations.
        /// </summary>
        public LocalizationCapability Localization =>
            mLocalization ??= new LocalizationCapability(this);

        /// <summary>
        /// Gets module-owned pool operations.
        /// </summary>
        public PoolCapability Pools => mPools ??= new PoolCapability(this);

        /// <summary>
        /// Gets module-owned asset operations.
        /// </summary>
        public AssetCapability Assets => mAssets ??= new AssetCapability(this);

        /// <summary>
        /// Gets module-owned configuration table operations.
        /// </summary>
        public TableCapability Tables => mTables ??= new TableCapability(this);

        /// <summary>
        /// Gets module-owned content update operations.
        /// </summary>
        public ContentCapability Content => mContent ??= new ContentCapability(this);

        /// <summary>
        /// Gets module-owned input operations.
        /// </summary>
        public InputCapability Input => mInput ??= new InputCapability(this);

        /// <summary>
        /// Gets module-owned networking operations.
        /// </summary>
        public NetworkCapability Network => mNetwork ??= new NetworkCapability(this);

        /// <summary>
        /// Gets shared application and scene state models.
        /// </summary>
        public ModelCapability Models => mModels ??= new ModelCapability(this);

        /// <summary>
        /// Creates and stores the logger for one concrete module.
        /// </summary>
        /// <param name="moduleType">The concrete module category type.</param>
        /// <param name="logLevel">The minimum severity accepted by the module.</param>
        /// <returns>The created category-bound logger.</returns>
        internal IModuleLogger Activate(Type moduleType, ELogLevel logLevel)
        {
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));
            if (!ReferenceEquals(mLogger, NullModuleLogger.Instance))
                throw new InvalidOperationException("The module context is already active.");

            mLogger = LoggerFactory.Create(moduleType, logLevel);
            return mLogger;
        }

        /// <summary>
        /// Resolves one required service with a feature-specific setup error.
        /// </summary>
        /// <typeparam name="TService">The required service contract.</typeparam>
        /// <param name="setupMessage">The setup guidance used when the service is missing.</param>
        /// <returns>The registered service instance.</returns>
        internal TService GetRequired<TService>(string setupMessage) where TService : class
        {
            if (Services.TryGet<TService>(out var service))
                return service;
            throw new InvalidOperationException(setupMessage);
        }

        /// <summary>
        /// Releases every resource owned through this module context.
        /// </summary>
        internal void Release()
        {
            try
            {
                Scope.Dispose();
            }
            finally
            {
                mLogger = NullModuleLogger.Instance;
            }
        }
    }
}

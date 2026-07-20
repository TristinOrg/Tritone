using System;
using System.Collections.Generic;
using Tritone.Models;
using Tritone.Flows;

namespace Tritone.Kernel
{
    /// <summary>
    /// Owns the service registry, module lifecycle, and per-frame update dispatch.
    /// </summary>
    public sealed class GameApplication : IDisposable, ISceneModuleService
    {
        /// <summary>
        /// Stores modules in dependency-safe startup order.
        /// </summary>
        private readonly ModuleRegistration[] mModules;

        // Stores independently owned contexts parallel to persistent modules.
        private readonly ModuleContext[] mModuleContexts;

        /// <summary>
        /// Maps registered scene module types to fresh-instance factories.
        /// </summary>
        private readonly Dictionary<Type, SceneModuleRegistration> mSceneModules;

        /// <summary>
        /// Stores the scene module entered immediately after persistent startup.
        /// </summary>
        private readonly Type mInitialSceneModuleType;

        /// <summary>
        /// Stores pre-update systems in stable execution order.
        /// </summary>
        private readonly IPreUpdateSystem[] mPreUpdateSystems;

        /// <summary>
        /// Stores normal update systems in stable execution order.
        /// </summary>
        private readonly IUpdateSystem[] mUpdateSystems;

        /// <summary>
        /// Stores late-update systems in stable execution order.
        /// </summary>
        private readonly ILateUpdateSystem[] mLateUpdateSystems;

        /// <summary>
        /// Stores fixed-update systems in stable execution order.
        /// </summary>
        private readonly IFixedUpdateSystem[] mFixedUpdateSystems;

        /// <summary>
        /// Stores all application-scoped services.
        /// </summary>
        private readonly ServiceRegistry mServices = new();

        /// <summary>
        /// Stores and owns all lazily created application and scene models.
        /// </summary>
        private readonly ModelService mModelService;

        /// <summary>
        /// Stores and owns the single active application flow.
        /// </summary>
        private readonly FlowService mFlowService;

        /// <summary>
        /// Stores the logger factory owned by this application.
        /// </summary>
        private readonly IModuleLoggerFactory mLoggerFactory;

        /// <summary>
        /// Tracks the number of modules that started successfully.
        /// </summary>
        private int mStartedModuleCount;

        /// <summary>
        /// Stores the currently active scene module.
        /// </summary>
        private IModule mActiveSceneModule;

        // Stores the independently owned context of the active scene module.
        private ModuleContext mActiveSceneModuleContext;

        /// <summary>
        /// Stores the active scene module type.
        /// </summary>
        private Type mActiveSceneModuleType;

        /// <summary>
        /// Stores the active scene module's optional pre-update interface.
        /// </summary>
        private IPreUpdateSystem   mScenePreUpdateSystem;

        /// <summary>
        /// Stores the active scene module's optional normal update interface.
        /// </summary>
        private IUpdateSystem      mSceneUpdateSystem;

        /// <summary>
        /// Stores the active scene module's optional late-update interface.
        /// </summary>
        private ILateUpdateSystem  mSceneLateUpdateSystem;

        /// <summary>
        /// Stores the active scene module's optional fixed-update interface.
        /// </summary>
        private IFixedUpdateSystem mSceneFixedUpdateSystem;

        /// <summary>
        /// Initializes an application with validated module registrations.
        /// </summary>
        /// <param name="modules">The modules in dependency-safe startup order.</param>
        /// <param name="sceneModules">The scene module factories available for dynamic activation.</param>
        /// <param name="models">The explicit model factories and ownership lifetimes.</param>
        /// <param name="flows">The explicit application flow factories.</param>
        /// <param name="initialSceneModuleType">The optional scene module entered after startup.</param>
        /// <param name="loggerFactory">The factory used to create and own module loggers.</param>
        internal GameApplication(ModuleRegistration[] modules,
                                 SceneModuleRegistration[] sceneModules,
                                 ModelRegistration[] models,
                                 FlowRegistration[] flows,
                                 Type initialSceneModuleType,
                                 IModuleLoggerFactory loggerFactory)
        {
            mModules                = modules ?? throw new ArgumentNullException(nameof(modules));
            mModuleContexts         = new ModuleContext[mModules.Length];
            mLoggerFactory          = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            mModelService           = new ModelService(
                models ?? throw new ArgumentNullException(nameof(models)));
            mFlowService            = new FlowService(
                flows ?? throw new ArgumentNullException(nameof(flows)));
            mSceneModules           = new(sceneModules?.Length ?? 0);
            mInitialSceneModuleType = initialSceneModuleType;

            if (sceneModules != null)
            {
                for (int i = 0, cnt = sceneModules.Length; i < cnt; i++)
                    mSceneModules.Add(sceneModules[i].ModuleType, sceneModules[i]);
            }

            mPreUpdateSystems   = CreateUpdateSystems<IPreUpdateSystem>(modules);
            mUpdateSystems      = CreateUpdateSystems<IUpdateSystem>(modules);
            mLateUpdateSystems  = CreateUpdateSystems<ILateUpdateSystem>(modules);
            mFixedUpdateSystems = CreateUpdateSystems<IFixedUpdateSystem>(modules);
        }

        /// <summary>
        /// Gets the current application lifecycle state.
        /// </summary>
        public EApplicationState State { get; private set; } = EApplicationState.Created;

        /// <summary>
        /// Gets the application-scoped service registry.
        /// </summary>
        public IServiceRegistry Services => mServices;

        /// <summary>
        /// Creates one independently owned context for a module lifecycle.
        /// </summary>
        /// <returns>A fresh module context sharing only application services.</returns>
        private ModuleContext CreateModuleContext()
        {
            return new ModuleContext(mServices, mLoggerFactory);
        }

        /// <inheritdoc />
        public Type ActiveModuleType => mActiveSceneModuleType;

        /// <summary>
        /// Configures and starts every module in dependency order.
        /// </summary>
        public void Start()
        {
            if (State != EApplicationState.Created)
                throw new InvalidOperationException($"Cannot start from state '{State}'.");

            State = EApplicationState.Starting;
            try
            {
                // Register modules first so configuration can resolve concrete module dependencies.
                mServices.AddSingleton<ISceneModuleService>(this);
                mServices.AddSingleton<IModelService>(mModelService);
                mServices.AddSingleton<IFlowService>(mFlowService);
                for (int i = 0, cnt = mModules.Length; i < cnt; i++)
                    mServices.AddSingleton(mModules[i].ModuleType, mModules[i].Module);
                for (int i = 0, cnt = mModules.Length; i < cnt; i++)
                {
                    var context       = CreateModuleContext();
                    mModuleContexts[i] = context;
                    mModules[i].Module.Configure(context);
                }

                mServices.Seal();
                for (int i = 0, cnt = mModules.Length; i < cnt; i++)
                {
                    mModules[i].Module.Start();
                    mStartedModuleCount++;
                }
                State = EApplicationState.Running;
                if (mInitialSceneModuleType != null)
                    SwitchModule(mInitialSceneModuleType);
            }
            catch (Exception startupException)
            {
                State = EApplicationState.Faulted;
                var shutdownException       = StopStartedModules();
                var infrastructureException = DisposeInfrastructure();
                if (shutdownException != null || infrastructureException != null)
                {
                    var errors = new List<Exception> { startupException };
                    if (shutdownException != null)
                        errors.Add(shutdownException);
                    if (infrastructureException != null)
                        errors.Add(infrastructureException);
                    throw new AggregateException("Application startup or rollback failed.", errors);
                }

                throw;
            }
        }

        /// <summary>
        /// Dispatches pre-update and normal update stages for one application frame.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        public void Update(in FrameTime time)
        {
            if (State != EApplicationState.Running)
                return;

            var scenePreUpdated = false;
            for (int i = 0, cnt = mPreUpdateSystems.Length; i < cnt; i++)
            {
                if (!scenePreUpdated && mScenePreUpdateSystem != null && mScenePreUpdateSystem.Order < mPreUpdateSystems[i].Order)
                {
                    mScenePreUpdateSystem.PreUpdate(in time);
                    scenePreUpdated = true;
                }
                mPreUpdateSystems[i].PreUpdate(in time);
            }
            if (!scenePreUpdated)
                mScenePreUpdateSystem?.PreUpdate(in time);

            mFlowService.Update(in time);

            var sceneUpdated = false;
            for (int i = 0, cnt = mUpdateSystems.Length; i < cnt; i++)
            {
                if (!sceneUpdated && mSceneUpdateSystem != null && mSceneUpdateSystem.Order < mUpdateSystems[i].Order)
                {
                    mSceneUpdateSystem.Update(in time);
                    sceneUpdated = true;
                }
                mUpdateSystems[i].Update(in time);
            }
            if (!sceneUpdated)
                mSceneUpdateSystem?.Update(in time);
        }

        /// <summary>
        /// Dispatches the late-update stage for one application frame.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        public void LateUpdate(in FrameTime time)
        {
            if (State != EApplicationState.Running)
                return;

            var sceneUpdated = false;
            for (int i = 0, cnt = mLateUpdateSystems.Length; i < cnt; i++)
            {
                if (!sceneUpdated && mSceneLateUpdateSystem != null && mSceneLateUpdateSystem.Order < mLateUpdateSystems[i].Order)
                {
                    mSceneLateUpdateSystem.LateUpdate(in time);
                    sceneUpdated = true;
                }
                mLateUpdateSystems[i].LateUpdate(in time);
            }
            if (!sceneUpdated)
                mSceneLateUpdateSystem?.LateUpdate(in time);
        }

        /// <summary>
        /// Dispatches the fixed-update stage for one simulation step.
        /// </summary>
        /// <param name="time">The immutable timing data for the current fixed update.</param>
        public void FixedUpdate(in FrameTime time)
        {
            if (State != EApplicationState.Running)
                return;

            var sceneUpdated = false;
            for (int i = 0, cnt = mFixedUpdateSystems.Length; i < cnt; i++)
            {
                if (!sceneUpdated && mSceneFixedUpdateSystem != null && mSceneFixedUpdateSystem.Order < mFixedUpdateSystems[i].Order)
                {
                    mSceneFixedUpdateSystem.FixedUpdate(in time);
                    sceneUpdated = true;
                }
                mFixedUpdateSystems[i].FixedUpdate(in time);
            }
            if (!sceneUpdated)
                mSceneFixedUpdateSystem?.FixedUpdate(in time);
        }

        /// <summary>
        /// Switches to a registered scene module and returns its fresh instance.
        /// </summary>
        /// <typeparam name="TModule">The concrete registered scene module type.</typeparam>
        /// <returns>The active scene module instance.</returns>
        public TModule SwitchModule<TModule>() where TModule : class, IModule
        {
            return (TModule)SwitchModule(typeof(TModule));
        }

        /// <inheritdoc />
        public object SwitchModule(Type moduleType)
        {
            if (State != EApplicationState.Running)
                throw new InvalidOperationException("Scene modules can only be switched while the application is running.");
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));
            if (moduleType == mActiveSceneModuleType)
                return mActiveSceneModule;
            if (!mSceneModules.TryGetValue(moduleType, out var registration))
                throw new InvalidOperationException($"Scene module '{moduleType.FullName}' is not registered.");

            ExitModule();
            var module = registration.Factory();
            if (module == null || module.GetType() != moduleType)
                throw new InvalidOperationException($"Scene module factory for '{moduleType.FullName}' returned an invalid instance.");

            var configured = false;
            var context    = CreateModuleContext();
            mServices.AddRuntime(moduleType, module);
            try
            {
                mModelService.BeginScene();
                module.Configure(context);
                configured = true;
                module.Start();
                SetActiveSceneModule(moduleType, module, context);
                return module;
            }
            catch (Exception startupException)
            {
                Exception cleanupException = null;
                if (configured)
                {
                    try
                    {
                        module.Stop();
                    }
                    catch (Exception exception)
                    {
                        cleanupException = exception;
                    }
                }
                try
                {
                    context.Release();
                }
                catch (Exception exception)
                {
                    cleanupException = cleanupException == null
                        ? exception
                        : new AggregateException(cleanupException, exception);
                }
                try
                {
                    mModelService.EndScene();
                }
                catch (Exception exception)
                {
                    cleanupException = cleanupException == null
                        ? exception
                        : new AggregateException(cleanupException, exception);
                }
                mServices.RemoveRuntime(moduleType, module);
                if (cleanupException != null)
                    throw new AggregateException("Scene module startup and cleanup failed.", startupException, cleanupException);
                throw;
            }
        }

        /// <inheritdoc />
        public void ExitModule()
        {
            if (mActiveSceneModule == null)
                return;

            var module      = mActiveSceneModule;
            var moduleType  = mActiveSceneModuleType;
            var context     = mActiveSceneModuleContext;
            SetActiveSceneModule(null, null, null);
            try
            {
                module.Stop();
            }
            finally
            {
                try
                {
                    context?.Release();
                }
                finally
                {
                    try
                    {
                        mModelService.EndScene();
                    }
                    finally
                    {
                        mServices.RemoveRuntime(moduleType, module);
                    }
                }
            }
        }

        /// <summary>
        /// Stops every started module in reverse startup order.
        /// </summary>
        public void Stop()
        {
            if (State == EApplicationState.Stopped)
                return;
            if (State == EApplicationState.Created)
            {
                State = EApplicationState.Stopped;
                var createdInfrastructureException = DisposeInfrastructure();
                if (createdInfrastructureException != null)
                    throw createdInfrastructureException;
                return;
            }

            State = EApplicationState.Stopping;
            Exception sceneException = null;
            try
            {
                ExitModule();
            }
            catch (Exception exception)
            {
                sceneException = exception;
            }
            var shutdownException       = StopStartedModules();
            var infrastructureException = DisposeInfrastructure();
            State = EApplicationState.Stopped;
            if (sceneException != null)
            {
                List<Exception> errors = new() { sceneException };
                if (shutdownException != null)
                    errors.Add(shutdownException);
                if (infrastructureException != null)
                    errors.Add(infrastructureException);
                throw new AggregateException("Application shutdown failed.", errors);
            }
            if (shutdownException != null && infrastructureException != null)
                throw new AggregateException("Application modules and infrastructure failed to stop.", shutdownException, infrastructureException);
            if (shutdownException != null)
                throw shutdownException;
            if (infrastructureException != null)
                throw infrastructureException;
        }

        /// <summary>
        /// Stops the application when it leaves an ownership scope.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Stops only modules that completed startup and collects cleanup failures.
        /// </summary>
        /// <returns>An aggregate cleanup exception when any module failed; otherwise, null.</returns>
        private AggregateException StopStartedModules()
        {
            List<Exception> errors = null;
            for (var i = mStartedModuleCount - 1; i >= 0; i--)
            {
                try
                {
                    mModules[i].Module.Stop();
                }
                catch (Exception exception)
                {
                    errors ??= new();
                    errors.Add(exception);
                }
            }
            mStartedModuleCount = 0;

            for (int i = mModuleContexts.Length - 1; i >= 0; i--)
            {
                var context = mModuleContexts[i];
                if (context == null)
                    continue;
                try
                {
                    context.Release();
                }
                catch (Exception exception)
                {
                    errors ??= new();
                    errors.Add(exception);
                }
                mModuleContexts[i] = null;
            }

            return errors == null
                ? null
                : new AggregateException("One or more modules failed to stop.", errors);
        }

        /// <summary>
        /// Releases application infrastructure after every module has stopped.
        /// </summary>
        /// <returns>The disposal exception when infrastructure cleanup failed; otherwise, null.</returns>
        private Exception DisposeInfrastructure()
        {
            List<Exception> errors = null;
            try
            {
                mFlowService.Dispose();
            }
            catch (Exception exception)
            {
                errors = new List<Exception> { exception };
            }
            try
            {
                mModelService.Dispose();
            }
            catch (Exception exception)
            {
                errors ??= new List<Exception>();
                errors.Add(exception);
            }
            try
            {
                mLoggerFactory.Dispose();
            }
            catch (Exception exception)
            {
                errors ??= new List<Exception>();
                errors.Add(exception);
            }
            return errors == null
                ? null
                : new AggregateException(
                    "Application infrastructure failed to release.",
                    errors);
        }

        /// <summary>
        /// Updates the active scene module and caches its optional update interfaces.
        /// </summary>
        private void SetActiveSceneModule(Type moduleType,
                                          IModule module,
                                          ModuleContext context)
        {
            mActiveSceneModuleType  = moduleType;
            mActiveSceneModule      = module;
            mActiveSceneModuleContext = context;
            mScenePreUpdateSystem   = module as IPreUpdateSystem;
            mSceneUpdateSystem      = module as IUpdateSystem;
            mSceneLateUpdateSystem  = module as ILateUpdateSystem;
            mSceneFixedUpdateSystem = module as IFixedUpdateSystem;
        }

        /// <summary>
        /// Collects and stably sorts systems for one update stage during application construction.
        /// </summary>
        /// <typeparam name="TSystem">The update stage interface to collect.</typeparam>
        /// <param name="modules">The modules in dependency-safe startup order.</param>
        /// <returns>The systems sorted by order while preserving module order for equal values.</returns>
        private static TSystem[] CreateUpdateSystems<TSystem>(ModuleRegistration[] modules)
            where TSystem : IOrderedUpdateSystem
        {
            List<TSystem> systems = new(modules.Length);
            for (int i = 0, cnt = modules.Length; i < cnt; i++)
            {
                if (modules[i].Module is not TSystem system)
                    continue;

                var insertionIndex = systems.Count;
                while (insertionIndex > 0 && systems[insertionIndex - 1].Order > system.Order)
                    insertionIndex--;

                systems.Insert(insertionIndex, system);
            }
            return systems.ToArray();
        }
    }
}

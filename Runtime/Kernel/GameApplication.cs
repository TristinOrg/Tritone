using System;
using System.Collections.Generic;

namespace Tritone.Kernel
{
    /// <summary>
    /// Owns the service registry, module lifecycle, and per-frame update dispatch.
    /// </summary>
    public sealed class GameApplication : IDisposable
    {
        /// <summary>
        /// Stores modules in dependency-safe startup order.
        /// </summary>
        private readonly ModuleRegistration[] mModules;

        /// <summary>
        /// Stores update systems in stable execution order.
        /// </summary>
        private readonly IUpdateSystem[] mUpdateSystems;

        /// <summary>
        /// Stores all application-scoped services.
        /// </summary>
        private readonly ServiceRegistry mServices = new ServiceRegistry();

        /// <summary>
        /// Stores immutable infrastructure passed to every module during configuration.
        /// </summary>
        private readonly ModuleContext mModuleContext;

        /// <summary>
        /// Stores the logger factory owned by this application.
        /// </summary>
        private readonly IModuleLoggerFactory mLoggerFactory;

        /// <summary>
        /// Tracks the number of modules that started successfully.
        /// </summary>
        private int mStartedModuleCount;

        /// <summary>
        /// Initializes an application with validated module registrations.
        /// </summary>
        /// <param name="modules">The modules in dependency-safe startup order.</param>
        /// <param name="loggerFactory">The factory used to create and own module loggers.</param>
        internal GameApplication(ModuleRegistration[] modules, IModuleLoggerFactory loggerFactory)
        {
            mModules       = modules ?? throw new ArgumentNullException(nameof(modules));
            mLoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            mModuleContext = new ModuleContext(mServices, mLoggerFactory);

            var updateSystems = new List<IUpdateSystem>(modules.Length);
            for (var i = 0; i < modules.Length; i++)
            {
                if (modules[i].Module is not IUpdateSystem updateSystem)
                    continue;

                var insertionIndex = updateSystems.Count;
                while (insertionIndex > 0 && updateSystems[insertionIndex - 1].Order > updateSystem.Order)
                    insertionIndex--;

                updateSystems.Insert(insertionIndex, updateSystem);
            }
            mUpdateSystems = updateSystems.ToArray();
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
                for (var i = 0; i < mModules.Length; i++)
                    mServices.AddSingleton(mModules[i].ModuleType, mModules[i].Module);
                for (var i = 0; i < mModules.Length; i++)
                    mModules[i].Module.Configure(mModuleContext);

                mServices.Seal();
                for (var i = 0; i < mModules.Length; i++)
                {
                    mModules[i].Module.Start();
                    mStartedModuleCount++;
                }
                State = EApplicationState.Running;
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
        /// Dispatches one frame update to every registered update system.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        public void Tick(in FrameTime time)
        {
            if (State != EApplicationState.Running)
                return;

            for (var i = 0; i < mUpdateSystems.Length; i++)
                mUpdateSystems[i].Update(in time);
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
            var shutdownException       = StopStartedModules();
            var infrastructureException = DisposeInfrastructure();
            State = EApplicationState.Stopped;
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
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }
            mStartedModuleCount = 0;

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
            try
            {
                mLoggerFactory.Dispose();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}

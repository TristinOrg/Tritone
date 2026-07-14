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
        /// Tracks the number of modules that started successfully.
        /// </summary>
        private int mStartedModuleCount;

        /// <summary>
        /// Initializes an application with validated module registrations.
        /// </summary>
        /// <param name="modules">The modules in dependency-safe startup order.</param>
        internal GameApplication(ModuleRegistration[] modules)
        {
            mModules = modules ?? throw new ArgumentNullException(nameof(modules));

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
                    mModules[i].Module.Configure(mServices);

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
                var shutdownException = StopStartedModules();
                if (shutdownException != null)
                    throw new AggregateException("Application startup and rollback both failed.", startupException, shutdownException);

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
                return;
            }

            State = EApplicationState.Stopping;
            var shutdownException = StopStartedModules();
            State = EApplicationState.Stopped;
            if (shutdownException != null)
                throw shutdownException;
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
    }
}

using System;
using System.Collections.Generic;
using Tritone.Kernel;

namespace Tritone.Entities
{
    /// <summary>
    /// Owns application and scene entity worlds and their deterministic boundaries.
    /// </summary>
    internal sealed class EntityService : IEntityService, IDisposable
    {
        // Stores application component registrations.
        private readonly ComponentRegistration[] mApplicationComponents;

        // Stores scene component registrations.
        private readonly ComponentRegistration[] mSceneComponents;

        // Stores application entity system registrations.
        private readonly EntitySystemRegistration[] mApplicationSystems;

        // Stores scene entity system registrations.
        private readonly EntitySystemRegistration[] mSceneSystems;

        // Stores the reserved capacity for every fresh world.
        private readonly int mInitialCapacity;

        // Stores the application world after startup.
        private EntityWorld mApplication;

        // Stores the active scene world.
        private EntityWorld mScene;

        // Indicates whether this service has completed disposal.
        private bool mDisposed;

        /// <summary>
        /// Initializes entity world configuration without creating lifecycle state.
        /// </summary>
        /// <param name="components">All validated component registrations.</param>
        /// <param name="systems">All validated system registrations.</param>
        /// <param name="initialCapacity">The reserved capacity for each fresh world.</param>
        internal EntityService(ComponentRegistration[] components,
                               EntitySystemRegistration[] systems,
                               int initialCapacity)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));
            if (systems == null)
                throw new ArgumentNullException(nameof(systems));
            if (initialCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            mApplicationComponents = FilterComponents(
                components,
                EEntityWorldLifetime.Application);
            mSceneComponents = FilterComponents(
                components,
                EEntityWorldLifetime.Scene);
            mApplicationSystems = FilterSystems(
                systems,
                EEntityWorldLifetime.Application);
            mSceneSystems = FilterSystems(
                systems,
                EEntityWorldLifetime.Scene);
            mInitialCapacity = initialCapacity;
        }

        /// <inheritdoc />
        public EntityWorld Application =>
            mApplication ??
            throw new InvalidOperationException(
                "The application entity world is not active.");

        /// <inheritdoc />
        public EntityWorld Scene =>
            mScene ??
            throw new InvalidOperationException(
                "A scene entity world requires an active scene module.");

        /// <summary>
        /// Creates the application world before modules configure.
        /// </summary>
        internal void Start()
        {
            ThrowIfDisposed();
            if (mApplication != null)
                throw new InvalidOperationException(
                    "The application entity world is already active.");
            mApplication = new EntityWorld(mApplicationComponents,
                                           mApplicationSystems,
                                           mInitialCapacity);
        }

        /// <summary>
        /// Creates one fresh scene world before a scene module configures.
        /// </summary>
        internal void BeginScene()
        {
            ThrowIfDisposed();
            if (mScene != null)
                throw new InvalidOperationException(
                    "A scene entity world is already active.");
            mScene = new EntityWorld(mSceneComponents,
                                     mSceneSystems,
                                     mInitialCapacity);
        }

        /// <summary>
        /// Releases the active scene world.
        /// </summary>
        internal void EndScene()
        {
            var scene = mScene;
            mScene = null;
            scene?.Dispose();
        }

        /// <summary>
        /// Updates application and scene worlds in lifetime order.
        /// </summary>
        /// <param name="time">The immutable timing data for the current frame.</param>
        internal void Update(in FrameTime time)
        {
            mApplication?.Update(in time);
            mScene?.Update(in time);
        }

        /// <summary>
        /// Releases scene and application worlds.
        /// </summary>
        public void Dispose()
        {
            if (mDisposed)
                return;
            mDisposed = true;

            List<Exception> errors = null;
            try
            {
                EndScene();
            }
            catch (Exception exception)
            {
                errors = new List<Exception> { exception };
            }

            var application = mApplication;
            mApplication = null;
            if (application != null)
            {
                try
                {
                    application.Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            if (errors != null)
                throw new AggregateException(
                    "One or more entity worlds failed to release.",
                    errors);
        }

        /// <summary>
        /// Filters component registrations for one world lifetime.
        /// </summary>
        private static ComponentRegistration[] FilterComponents(
            ComponentRegistration[] registrations,
            EEntityWorldLifetime lifetime)
        {
            var count = 0;
            for (int i = 0, cnt = registrations.Length; i < cnt; i++)
            {
                if (registrations[i].Lifetime == lifetime)
                    count++;
            }

            ComponentRegistration[] result = new ComponentRegistration[count];
            var index = 0;
            for (int i = 0, cnt = registrations.Length; i < cnt; i++)
            {
                if (registrations[i].Lifetime == lifetime)
                    result[index++] = registrations[i];
            }
            return result;
        }

        /// <summary>
        /// Filters entity system registrations for one world lifetime.
        /// </summary>
        private static EntitySystemRegistration[] FilterSystems(
            EntitySystemRegistration[] registrations,
            EEntityWorldLifetime lifetime)
        {
            var count = 0;
            for (int i = 0, cnt = registrations.Length; i < cnt; i++)
            {
                if (registrations[i].Lifetime == lifetime)
                    count++;
            }

            EntitySystemRegistration[] result = new EntitySystemRegistration[count];
            var index = 0;
            for (int i = 0, cnt = registrations.Length; i < cnt; i++)
            {
                if (registrations[i].Lifetime == lifetime)
                    result[index++] = registrations[i];
            }
            return result;
        }

        /// <summary>
        /// Rejects access after application shutdown.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(EntityService));
        }
    }
}

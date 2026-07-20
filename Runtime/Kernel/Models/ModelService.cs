using System;
using System.Collections.Generic;

namespace Tritone.Models
{
    /// <summary>
    /// Lazily creates models and releases them at deterministic ownership boundaries.
    /// </summary>
    internal sealed class ModelService : IModelService, IDisposable
    {
        // Stores immutable model registrations by concrete type.
        private readonly Dictionary<Type, ModelRegistration> mRegistrations;

        // Stores created application models by concrete type.
        private readonly Dictionary<Type, IModel> mApplicationModels = new();

        // Stores application models in successful creation order.
        private readonly List<IModel> mApplicationOrder = new();

        // Stores created models for the active scene module.
        private readonly Dictionary<Type, IModel> mSceneModels = new();

        // Stores scene models in successful creation order.
        private readonly List<IModel> mSceneOrder = new();

        // Indicates whether a scene model lifetime is currently active.
        private bool mSceneActive;

        // Indicates whether the service has completed disposal.
        private bool mDisposed;

        /// <summary>
        /// Initializes the service with explicit immutable registrations.
        /// </summary>
        /// <param name="registrations">The validated model registrations.</param>
        internal ModelService(ModelRegistration[] registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            mRegistrations = new Dictionary<Type, ModelRegistration>(registrations.Length);
            for (int i = 0, cnt = registrations.Length; i < cnt; i++)
                mRegistrations.Add(registrations[i].ModelType, registrations[i]);
        }

        /// <inheritdoc />
        public TModel Get<TModel>() where TModel : class, IModel
        {
            return (TModel)Get(typeof(TModel));
        }

        /// <inheritdoc />
        public IModel Get(Type modelType)
        {
            ThrowIfDisposed();
            if (modelType == null)
                throw new ArgumentNullException(nameof(modelType));
            if (!mRegistrations.TryGetValue(modelType, out var registration))
                throw new InvalidOperationException(
                    $"Model '{modelType.FullName}' is not registered.");

            Dictionary<Type, IModel> models;
            List<IModel> order;
            if (registration.Lifetime == EModelLifetime.Application)
            {
                models = mApplicationModels;
                order  = mApplicationOrder;
            }
            else
            {
                if (!mSceneActive)
                    throw new InvalidOperationException(
                        $"Scene model '{modelType.FullName}' requires an active scene module.");
                models = mSceneModels;
                order  = mSceneOrder;
            }

            if (models.TryGetValue(modelType, out var existing))
                return existing;

            var model = registration.Factory();
            if (model == null || model.GetType() != modelType)
                throw new InvalidOperationException(
                    $"Model factory for '{modelType.FullName}' returned an invalid instance.");

            try
            {
                model.Initialize();
            }
            catch (Exception initializationException)
            {
                try
                {
                    model.Dispose();
                }
                catch (Exception disposalException)
                {
                    throw new AggregateException(
                        "Model initialization and cleanup failed.",
                        initializationException,
                        disposalException);
                }
                throw;
            }

            models.Add(modelType, model);
            order.Add(model);
            return model;
        }

        /// <inheritdoc />
        public bool Reset<TModel>() where TModel : class, IModel
        {
            return Reset(typeof(TModel));
        }

        /// <inheritdoc />
        public bool Reset(Type modelType)
        {
            ThrowIfDisposed();
            if (modelType == null)
                throw new ArgumentNullException(nameof(modelType));
            if (!mRegistrations.TryGetValue(modelType, out var registration))
                throw new InvalidOperationException(
                    $"Model '{modelType.FullName}' is not registered.");

            var models = registration.Lifetime == EModelLifetime.Application
                ? mApplicationModels
                : mSceneModels;
            if (!models.TryGetValue(modelType, out var model))
                return false;

            model.Reset();
            return true;
        }

        /// <summary>
        /// Opens a fresh scene ownership boundary.
        /// </summary>
        internal void BeginScene()
        {
            ThrowIfDisposed();
            if (mSceneActive)
                throw new InvalidOperationException("A scene model lifetime is already active.");

            mSceneActive = true;
        }

        /// <summary>
        /// Releases all models created for the active scene in reverse order.
        /// </summary>
        internal void EndScene()
        {
            if (!mSceneActive)
                return;

            mSceneActive = false;
            var exception = DisposeModels(mSceneOrder, mSceneModels);
            if (exception != null)
                throw exception;
        }

        /// <summary>
        /// Releases scene models followed by application models.
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

            var applicationException = DisposeModels(
                mApplicationOrder,
                mApplicationModels);
            if (applicationException != null)
            {
                errors ??= new List<Exception>();
                errors.Add(applicationException);
            }

            if (errors != null)
                throw new AggregateException(
                    "One or more models failed to release.",
                    errors);
        }

        /// <summary>
        /// Releases one model collection in reverse creation order.
        /// </summary>
        /// <param name="order">The successful model creation order.</param>
        /// <param name="models">The model lookup cleared after release.</param>
        /// <returns>An aggregate exception when any model failed to release; otherwise, null.</returns>
        private static AggregateException DisposeModels(
            List<IModel> order,
            Dictionary<Type, IModel> models)
        {
            List<Exception> errors = null;
            for (int i = order.Count - 1; i >= 0; i--)
            {
                try
                {
                    order[i].Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            order.Clear();
            models.Clear();
            return errors == null
                ? null
                : new AggregateException(
                    "One or more models failed to release.",
                    errors);
        }

        /// <summary>
        /// Rejects access after the application has released this service.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(ModelService));
        }
    }
}

using System;
using Tritone.Models;

namespace Tritone.Kernel
{
    /// <summary>
    /// Exposes shared state models without transferring their ownership to a consumer module.
    /// </summary>
    public sealed class ModelCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Stores the shared model service resolved on first use.
        private IModelService mService;

        /// <summary>
        /// Initializes model operations for one module context.
        /// </summary>
        /// <param name="context">The module context that resolves the shared service.</param>
        internal ModelCapability(ModuleContext context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets or lazily creates one registered model.
        /// </summary>
        /// <typeparam name="TModel">The concrete registered model type.</typeparam>
        /// <returns>The shared model instance for its configured lifetime.</returns>
        public TModel Get<TModel>() where TModel : class, IModel
        {
            return Service.Get<TModel>();
        }

        /// <summary>
        /// Gets or lazily creates one registered model by runtime type.
        /// </summary>
        /// <param name="modelType">The concrete registered model type.</param>
        /// <returns>The shared model instance for its configured lifetime.</returns>
        public IModel Get(Type modelType)
        {
            return Service.Get(modelType);
        }

        /// <summary>
        /// Resets one created model without replacing its shared instance.
        /// </summary>
        /// <typeparam name="TModel">The concrete registered model type.</typeparam>
        /// <returns>True when the model had been created and was reset; otherwise, false.</returns>
        public bool Reset<TModel>() where TModel : class, IModel
        {
            return Service.Reset<TModel>();
        }

        /// <summary>
        /// Gets the configured shared model service.
        /// </summary>
        private IModelService Service =>
            mService ??= mContext.GetRequired<IModelService>(
                "Model services are not configured. Register at least one model on GameApplicationBuilder.");
    }
}

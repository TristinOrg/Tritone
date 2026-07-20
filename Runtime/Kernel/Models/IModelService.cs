using System;

namespace Tritone.Models
{
    /// <summary>
    /// Resolves explicitly registered shared state models.
    /// </summary>
    public interface IModelService
    {
        /// <summary>
        /// Gets or lazily creates one registered model.
        /// </summary>
        /// <typeparam name="TModel">The concrete registered model type.</typeparam>
        /// <returns>The shared model instance for its configured lifetime.</returns>
        TModel Get<TModel>() where TModel : class, IModel;

        /// <summary>
        /// Gets or lazily creates one registered model by runtime type.
        /// </summary>
        /// <param name="modelType">The concrete registered model type.</param>
        /// <returns>The shared model instance for its configured lifetime.</returns>
        IModel Get(Type modelType);

        /// <summary>
        /// Resets one created model without replacing its shared instance.
        /// </summary>
        /// <typeparam name="TModel">The concrete registered model type.</typeparam>
        /// <returns>True when the model had been created and was reset; otherwise, false.</returns>
        bool Reset<TModel>() where TModel : class, IModel;

        /// <summary>
        /// Resets one created model by runtime type without replacing its shared instance.
        /// </summary>
        /// <param name="modelType">The concrete registered model type.</param>
        /// <returns>True when the model had been created and was reset; otherwise, false.</returns>
        bool Reset(Type modelType);
    }
}

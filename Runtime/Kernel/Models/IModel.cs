using System;

namespace Tritone.Models
{
    /// <summary>
    /// Defines deterministic initialization, reset, and release for shared game state.
    /// </summary>
    public interface IModel : IDisposable
    {
        /// <summary>
        /// Initializes the model after its factory creates it.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Restores the model to its initial runtime state.
        /// </summary>
        void Reset();
    }
}

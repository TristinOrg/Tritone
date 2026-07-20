using Tritone.Saves;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides shared save operations without owning storage implementation.
    /// </summary>
    public sealed class SaveCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes save operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal SaveCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Writes one strongly typed save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <param name="value">The complete save data.</param>
        public void Save<T>(string slot, T value) where T : class
        {
            GetService().Save(slot, value);
        }

        /// <summary>
        /// Loads one required strongly typed save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>The loaded save data.</returns>
        public T Load<T>(string slot) where T : class
        {
            return GetService().Load<T>(slot);
        }

        /// <summary>
        /// Attempts to load one optional strongly typed save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <param name="value">The loaded value when found; otherwise, null.</param>
        /// <returns>True when the slot was loaded; otherwise, false.</returns>
        public bool TryLoad<T>(string slot, out T value) where T : class
        {
            return GetService().TryLoad(slot, out value);
        }

        /// <summary>
        /// Deletes one local save slot.
        /// </summary>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>True when slot data was deleted; otherwise, false.</returns>
        public bool Delete(string slot)
        {
            return GetService().Delete(slot);
        }

        /// <summary>
        /// Gets the configured save service.
        /// </summary>
        /// <returns>The application save service.</returns>
        private ISaveService GetService()
        {
            return mContext.GetRequired<ISaveService>(
                "Save infrastructure is not configured. Call builder.UseSaves() before adding game modules.");
        }
    }
}

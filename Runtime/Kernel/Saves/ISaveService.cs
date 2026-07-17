namespace Tritone.Saves
{
    /// <summary>
    /// Provides atomic strongly typed local save slots.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Determines whether one save slot exists.
        /// </summary>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>True when the slot exists; otherwise, false.</returns>
        bool Exists(string slot);

        /// <summary>
        /// Atomically writes one save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <param name="value">The complete save data.</param>
        void Save<T>(string slot, T value) where T : class;

        /// <summary>
        /// Loads one required save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>The deserialized save data.</returns>
        T Load<T>(string slot) where T : class;

        /// <summary>
        /// Attempts to load one optional save slot.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <param name="value">The loaded value when found; otherwise, null.</param>
        /// <returns>True when the slot exists and was loaded; otherwise, false.</returns>
        bool TryLoad<T>(string slot, out T value) where T : class;

        /// <summary>
        /// Deletes one save slot and any recovery file.
        /// </summary>
        /// <param name="slot">The caller-defined slot name.</param>
        /// <returns>True when any slot data was deleted; otherwise, false.</returns>
        bool Delete(string slot);
    }
}

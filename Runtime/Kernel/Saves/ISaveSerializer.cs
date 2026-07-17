namespace Tritone.Saves
{
    /// <summary>
    /// Converts strongly typed save data to and from durable bytes.
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>
        /// Serializes one save object.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="value">The save object.</param>
        /// <returns>The complete serialized bytes.</returns>
        byte[] Serialize<T>(T value) where T : class;

        /// <summary>
        /// Deserializes one save object.
        /// </summary>
        /// <typeparam name="T">The save data type.</typeparam>
        /// <param name="data">The complete serialized bytes.</param>
        /// <returns>The deserialized save object.</returns>
        T Deserialize<T>(byte[] data) where T : class;
    }
}

namespace Tritone.Tables
{
    /// <summary>
    /// Converts one configuration asset into a strongly typed row array.
    /// </summary>
    public interface ITableDeserializer
    {
        /// <summary>
        /// Deserializes one complete row array.
        /// </summary>
        /// <typeparam name="TRow">The configuration row type.</typeparam>
        /// <param name="data">The complete configuration asset bytes.</param>
        /// <returns>The deserialized rows in source order.</returns>
        TRow[] Deserialize<TRow>(byte[] data);
    }
}

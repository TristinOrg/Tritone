namespace Tritone.Content
{
    /// <summary>
    /// Converts immutable content manifests to and from a transport format.
    /// </summary>
    public interface IContentManifestSerializer
    {
        /// <summary>
        /// Deserializes and validates one content manifest.
        /// </summary>
        /// <param name="content">The complete serialized manifest content.</param>
        /// <returns>A validated immutable content manifest.</returns>
        ContentManifest Deserialize(string content);

        /// <summary>
        /// Serializes one validated content manifest for durable local storage.
        /// </summary>
        /// <param name="manifest">The manifest to serialize.</param>
        /// <returns>The complete serialized manifest content.</returns>
        string Serialize(ContentManifest manifest);
    }
}

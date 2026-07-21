using Tritone.Content;

namespace Tritone.Editor.ContentBuild
{
    /// <summary>
    /// Reports the immutable outputs of one completed content build.
    /// </summary>
    public readonly struct ContentBuildResult
    {
        /// <summary>
        /// Gets the generated and validated runtime manifest.
        /// </summary>
        public ContentManifest Manifest { get; }

        /// <summary>
        /// Gets the complete generated manifest file path.
        /// </summary>
        public string ManifestPath { get; }

        /// <summary>
        /// Initializes one completed content build result.
        /// </summary>
        /// <param name="manifest">The generated and validated runtime manifest.</param>
        /// <param name="manifestPath">The complete generated manifest file path.</param>
        public ContentBuildResult(ContentManifest manifest, string manifestPath)
        {
            Manifest     = manifest;
            ManifestPath = manifestPath;
        }
    }
}

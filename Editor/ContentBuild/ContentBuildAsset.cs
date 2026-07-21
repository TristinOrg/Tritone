namespace Tritone.Editor.ContentBuild
{
    /// <summary>
    /// Maps one public content address to an asset path included in an AssetBundle.
    /// </summary>
    public readonly struct ContentBuildAsset
    {
        /// <summary>
        /// Gets the public address used by the runtime asset service.
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// Gets the project-relative asset path included in an AssetBundle.
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// Initializes one content build asset mapping.
        /// </summary>
        /// <param name="address">The public address used by the runtime asset service.</param>
        /// <param name="assetPath">The project-relative asset path included in an AssetBundle.</param>
        public ContentBuildAsset(string address, string assetPath)
        {
            Address   = address;
            AssetPath = assetPath;
        }
    }
}

namespace Tritone.Content
{
    /// <summary>
    /// Maps one public asset address to its exact name inside a content bundle.
    /// </summary>
    public sealed class ContentAsset
    {
        // Gets the stable address passed to the asset service.
        public string Address { get; }

        // Gets the logical name of the containing bundle.
        public string BundleName { get; }

        // Gets the exact asset name stored inside the bundle.
        public string AssetName { get; }

        /// <summary>
        /// Initializes one immutable addressed asset definition.
        /// </summary>
        /// <param name="address">The stable public asset address.</param>
        /// <param name="bundleName">The logical name of the containing bundle.</param>
        /// <param name="assetName">The exact asset name stored inside the bundle.</param>
        public ContentAsset(string address, string bundleName, string assetName)
        {
            ContentValidation.ValidateValue(address, nameof(address));
            ContentValidation.ValidateValue(bundleName, nameof(bundleName));
            ContentValidation.ValidateValue(assetName, nameof(assetName));

            Address    = address;
            BundleName = bundleName;
            AssetName  = assetName;
        }
    }
}

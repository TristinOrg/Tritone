using System.Threading.Tasks;
using Tritone.Assets;

namespace Tritone.Kernel
{
    /// <summary>
    /// Provides asset operations whose ownership follows one module context.
    /// </summary>
    public sealed class AssetCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific asset scope.
        private IAssetScope mScope;

        /// <summary>
        /// Initializes asset operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal AssetCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Loads and owns one asset synchronously.
        /// </summary>
        /// <typeparam name="T">The requested reference type.</typeparam>
        /// <param name="path">The provider-specific asset path.</param>
        /// <returns>The loaded asset.</returns>
        public T Load<T>(string path) where T : class
        {
            return GetScope().Load<T>(path);
        }

        /// <summary>
        /// Loads and owns one asset asynchronously.
        /// </summary>
        /// <typeparam name="T">The requested reference type.</typeparam>
        /// <param name="path">The provider-specific asset path.</param>
        /// <returns>A task containing the loaded asset.</returns>
        public Task<T> LoadAsync<T>(string path) where T : class
        {
            return GetScope().LoadAsync<T>(path);
        }

        /// <summary>
        /// Releases one asset before the module lifetime ends.
        /// </summary>
        /// <typeparam name="T">The loaded asset type.</typeparam>
        /// <param name="asset">The asset reference to release.</param>
        /// <returns>True when this capability owned the reference; otherwise, false.</returns>
        public bool Release<T>(T asset) where T : class
        {
            return mScope != null && mScope.Release(asset);
        }

        /// <summary>
        /// Gets or creates the asset scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned asset scope.</returns>
        private IAssetScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IAssetService>(
                "Asset infrastructure is not configured. Call builder.UseAssets() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }
}

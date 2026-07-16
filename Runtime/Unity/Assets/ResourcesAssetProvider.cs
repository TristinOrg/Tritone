using System;
using System.Threading.Tasks;
using Tritone.Assets;
using UnityEngine;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Loads assets from Unity Resources while preserving the common Tritone asset API.
    /// </summary>
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        /// <inheritdoc />
        public object Load(string path, Type assetType)
        {
            return Resources.Load(path, assetType);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string path, Type assetType)
        {
            var request = Resources.LoadAsync(path, assetType);
            if (request.isDone)
                return Task.FromResult((object)request.asset);

            TaskCompletionSource<object> completion = new();
            request.completed += _ => completion.TrySetResult(request.asset);
            return completion.Task;
        }

        /// <inheritdoc />
        public void Release(object asset)
        {
            // Resources owns its internal cache. Tritone only drops its reference here to avoid forced global unload hitches.
        }
    }
}

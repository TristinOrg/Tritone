using System;
using System.Collections.Generic;

namespace Tritone.Networking
{
    /// <summary>
    /// Releases all message callbacks registered by one owner.
    /// </summary>
    internal sealed class NetworkScope : INetworkScope
    {
        private NetworkModule mModule;
        private readonly List<NetworkBinding> mBindings = new();

        internal NetworkScope(NetworkModule module)
        {
            mModule = module;
        }

        public void Bind<T>(Action<T> callback) where T : class
        {
            if (mModule == null)
                throw new ObjectDisposedException(nameof(NetworkScope));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            NetworkBinding binding = new()
            {
                MessageType = typeof(T),
                Callback    = message => callback((T)message)
            };
            mBindings.Add(binding);
            mModule.Add(binding);
        }

        public void Dispose()
        {
            if (mModule == null)
                return;
            for (int i = mBindings.Count - 1; i >= 0; i--)
                mModule.Remove(mBindings[i]);
            mBindings.Clear();
            mModule = null;
        }
    }
}

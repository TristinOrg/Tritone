using System;
using System.Collections.Generic;

namespace Tritone.Kernel
{
    /// <summary>
    /// Owns disposable resources that share one module lifetime.
    /// </summary>
    public sealed class ModuleScope : IDisposable
    {
        // Stores owned resources in acquisition order.
        private List<IDisposable> mResources;

        // Indicates whether this scope has completed disposal.
        private bool mDisposed;

        /// <summary>
        /// Adds one resource to this module lifetime.
        /// </summary>
        /// <typeparam name="T">The disposable resource type.</typeparam>
        /// <param name="resource">The resource whose ownership is transferred to this scope.</param>
        /// <returns>The same resource for concise creation and ownership.</returns>
        public T Own<T>(T resource) where T : class, IDisposable
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            if (mDisposed)
                throw new ObjectDisposedException(nameof(ModuleScope));

            mResources ??= new List<IDisposable>(4);
            mResources.Add(resource);
            return resource;
        }

        /// <summary>
        /// Releases every owned resource in reverse acquisition order.
        /// </summary>
        public void Dispose()
        {
            if (mDisposed)
                return;

            mDisposed = true;
            if (mResources == null)
                return;

            List<Exception> exceptions = null;
            for (int i = mResources.Count - 1; i >= 0; i--)
            {
                try
                {
                    mResources[i].Dispose();
                }
                catch (Exception exception)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
            }
            mResources.Clear();
            mResources = null;

            if (exceptions != null)
                throw new AggregateException(
                    "One or more module-owned resources failed to release.",
                    exceptions);
        }
    }
}

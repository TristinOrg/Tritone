using Tritone.Pooling;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides pooled object operations whose ownership follows one module context.
    /// </summary>
    public sealed class PoolCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Lazily stores the domain-specific pool scope.
        private IPoolScope mScope;

        /// <summary>
        /// Initializes pool operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal PoolCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Rents and owns one plain C# object.
        /// </summary>
        /// <typeparam name="T">The pooled object type.</typeparam>
        /// <returns>The rented object.</returns>
        public T Rent<T>() where T : class, new()
        {
            return GetScope().Rent<T>();
        }

        /// <summary>
        /// Returns one rented object before the module lifetime ends.
        /// </summary>
        /// <typeparam name="T">The pooled object type.</typeparam>
        /// <param name="instance">The rented object to return.</param>
        /// <returns>True when this capability owned the object; otherwise, false.</returns>
        public bool Return<T>(T instance) where T : class
        {
            return mScope != null && mScope.Return(instance);
        }

        /// <summary>
        /// Spawns and owns one prefab instance.
        /// </summary>
        /// <typeparam name="T">The prefab reference type.</typeparam>
        /// <param name="prefab">The prefab to spawn.</param>
        /// <param name="parent">The optional Unity parent object.</param>
        /// <returns>The spawned instance.</returns>
        public T Spawn<T>(T prefab, object parent) where T : class
        {
            return GetScope().Spawn(prefab, parent);
        }

        /// <summary>
        /// Despawns one prefab instance before the module lifetime ends.
        /// </summary>
        /// <typeparam name="T">The spawned reference type.</typeparam>
        /// <param name="instance">The spawned instance to return.</param>
        /// <returns>True when this capability owned the instance; otherwise, false.</returns>
        public bool Despawn<T>(T instance) where T : class
        {
            return mScope != null && mScope.Despawn(instance);
        }

        /// <summary>
        /// Gets or creates the pool scope owned by this capability.
        /// </summary>
        /// <returns>The module-owned pool scope.</returns>
        private IPoolScope GetScope()
        {
            if (mScope != null)
                return mScope;
            var service = mContext.GetRequired<IPoolService>(
                "Pool infrastructure is not configured. Call builder.UsePools() before adding game modules.");
            mScope = mContext.Scope.Own(service.CreateScope());
            return mScope;
        }
    }
}

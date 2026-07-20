using System;
using Tritone.Entities;

namespace Tritone.Kernel
{
    /// <summary>
    /// Exposes application and active scene entity worlds to one module context.
    /// </summary>
    public sealed class EntityCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        // Stores the shared entity service resolved on first use.
        private IEntityService mService;

        /// <summary>
        /// Initializes entity access for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal EntityCapability(ModuleContext context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the world that survives scene module changes.
        /// </summary>
        public EntityWorld Application => Service.Application;

        /// <summary>
        /// Gets the world owned by the active scene module.
        /// </summary>
        public EntityWorld Scene => Service.Scene;

        /// <summary>
        /// Gets shared entity world infrastructure.
        /// </summary>
        private IEntityService Service =>
            mService ??= mContext.GetRequired<IEntityService>(
                "Entity infrastructure is unavailable.");
    }
}

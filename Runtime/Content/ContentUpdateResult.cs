namespace Tritone.Content
{
    /// <summary>
    /// Describes the active content state after one successful update check.
    /// </summary>
    public sealed class ContentUpdateResult
    {
        // Gets the manifest active before the update, or null on first installation.
        public ContentManifest PreviousManifest { get; }

        // Gets the remote manifest made active by this operation.
        public ContentManifest ActiveManifest { get; }

        // Gets the deterministic plan used by this operation.
        public ContentUpdatePlan Plan { get; }

        // Gets whether files or active manifest metadata changed.
        public bool Updated => Plan.HasChanges;

        /// <summary>
        /// Initializes one immutable successful update result.
        /// </summary>
        /// <param name="previousManifest">The previously active local manifest, or null.</param>
        /// <param name="activeManifest">The manifest active after the operation.</param>
        /// <param name="plan">The update plan that was executed.</param>
        internal ContentUpdateResult(ContentManifest previousManifest,
                                     ContentManifest activeManifest,
                                     ContentUpdatePlan plan)
        {
            PreviousManifest = previousManifest;
            ActiveManifest   = activeManifest;
            Plan             = plan;
        }
    }
}

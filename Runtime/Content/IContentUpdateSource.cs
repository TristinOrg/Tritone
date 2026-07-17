using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Content
{
    /// <summary>
    /// Retrieves a remote manifest and streams remote bundle files into caller-owned paths.
    /// </summary>
    public interface IContentUpdateSource
    {
        /// <summary>
        /// Retrieves the complete serialized remote manifest.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel remote work.</param>
        /// <returns>A task containing the serialized remote manifest.</returns>
        Task<string> GetManifestAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Downloads one complete bundle into a temporary destination file.
        /// </summary>
        /// <param name="bundle">The remote bundle definition to download.</param>
        /// <param name="destinationPath">The caller-owned temporary destination path.</param>
        /// <param name="progress">The optional callback receiving current file bytes.</param>
        /// <param name="cancellationToken">The token used to cancel remote work.</param>
        /// <returns>A task completed after the destination file has been closed.</returns>
        Task DownloadBundleAsync(ContentBundle bundle,
                                 string destinationPath,
                                 Action<long> progress,
                                 CancellationToken cancellationToken);
    }
}

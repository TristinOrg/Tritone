using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Content
{
    /// <summary>
    /// Executes serialized, transactional content updates with streaming verification and crash recovery.
    /// </summary>
    public sealed class ContentUpdater
    {
        // Defines private transaction subdirectories and marker files.
        private const string DownloadsDirectoryName = "downloads";
        private const string BackupsDirectoryName   = "backups";
        private const string CreatedDirectoryName   = "created";
        private const string ManifestNewFileName    = "manifest.new";
        private const string ManifestBackupFileName = "manifest.backup";
        private const string ManifestCreatedMarker  = "manifest.created";
        private const string CommittedMarker        = "committed";

        // Stores UTF-8 encoding without a byte-order mark.
        private static readonly UTF8Encoding sEncoding = new(false);

        // Stores validated local content paths.
        private readonly ContentUpdateSettings mSettings;

        // Retrieves remote manifest and bundle data.
        private readonly IContentUpdateSource mSource;

        // Converts manifests to and from their transport format.
        private readonly IContentManifestSerializer mSerializer;

        // Serializes update operations sharing the same local storage.
        private readonly SemaphoreSlim mUpdateLock = new(1, 1);

        /// <summary>
        /// Initializes one transactional content updater.
        /// </summary>
        /// <param name="settings">The validated local storage settings.</param>
        /// <param name="source">The source used to retrieve remote content.</param>
        /// <param name="serializer">The serializer used for remote and local manifests.</param>
        public ContentUpdater(ContentUpdateSettings settings,
                              IContentUpdateSource source,
                              IContentManifestSerializer serializer)
        {
            mSettings   = settings ?? throw new ArgumentNullException(nameof(settings));
            mSource     = source ?? throw new ArgumentNullException(nameof(source));
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Checks, downloads, verifies, commits, and activates the latest remote content.
        /// </summary>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="cancellationToken">The token used to cancel work before the commit stage.</param>
        /// <returns>A task containing the active manifest and executed update plan.</returns>
        public async Task<ContentUpdateResult> UpdateAsync(Action<ContentUpdateProgress> progress = null,
                                                           CancellationToken cancellationToken = default)
        {
            await mUpdateLock.WaitAsync(cancellationToken);
            try
            {
                return await UpdateInternalAsync(progress, cancellationToken);
            }
            finally
            {
                mUpdateLock.Release();
            }
        }

        /// <summary>
        /// Executes one serialized update while the update lock is held.
        /// </summary>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="cancellationToken">The token used to cancel work before commit.</param>
        /// <returns>A task containing the successful update result.</returns>
        private async Task<ContentUpdateResult> UpdateInternalAsync(Action<ContentUpdateProgress> progress,
                                                                    CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(mSettings.RootPath);
            RecoverTransaction();
            Report(progress, EContentUpdateStage.Checking, null, 0, 0, 0, 0);

            cancellationToken.ThrowIfCancellationRequested();
            var localManifest = LoadLocalManifest();
            var remoteContent = await mSource.GetManifestAsync(cancellationToken);
            var remoteManifest = mSerializer.Deserialize(remoteContent) ??
                                 throw new InvalidDataException("The manifest serializer returned null.");
            var plan = ContentUpdatePlanner.CreatePlan(localManifest,
                                                       remoteManifest,
                                                       CanReuseLocalFile);
            if (!plan.HasChanges)
            {
                Report(progress, EContentUpdateStage.Completed, null, 0, 0, 0, 0);
                return new ContentUpdateResult(localManifest, remoteManifest, plan);
            }

            PrepareTransaction(remoteManifest);
            try
            {
                await DownloadAndVerifyAsync(plan, progress, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                Report(progress,
                       EContentUpdateStage.Committing,
                       null,
                       plan.DownloadBytes,
                       plan.DownloadBytes,
                       plan.Downloads.Count,
                       plan.Downloads.Count);
                Commit(plan);
            }
            catch (Exception exception)
            {
                try
                {
                    RecoverTransaction();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("Content update failed and transaction rollback also failed.",
                                                 exception,
                                                 rollbackException);
                }
                throw;
            }

            TryDeleteCommittedTransaction();
            Report(progress,
                   EContentUpdateStage.Completed,
                   null,
                   plan.DownloadBytes,
                   plan.DownloadBytes,
                   plan.Downloads.Count,
                   plan.Downloads.Count);
            return new ContentUpdateResult(localManifest, remoteManifest, plan);
        }

        /// <summary>
        /// Loads and validates the active local manifest when one exists.
        /// </summary>
        /// <returns>The active local manifest, or null on first installation.</returns>
        private ContentManifest LoadLocalManifest()
        {
            if (!File.Exists(mSettings.ManifestPath))
                return null;

            var content = File.ReadAllText(mSettings.ManifestPath, sEncoding);
            return mSerializer.Deserialize(content) ??
                   throw new InvalidDataException("The local manifest serializer returned null.");
        }

        /// <summary>
        /// Confirms that a manifest-matching local file physically exists with the expected size.
        /// </summary>
        /// <param name="bundle">The remote bundle whose local file may be reused.</param>
        /// <returns>True when the local file exists with the manifest size.</returns>
        private bool CanReuseLocalFile(ContentBundle bundle)
        {
            var path = mSettings.ResolveBundlePath(bundle.FileName);
            return File.Exists(path) && new FileInfo(path).Length == bundle.Size;
        }

        /// <summary>
        /// Creates clean transaction storage and durably records the target manifest.
        /// </summary>
        /// <param name="remoteManifest">The manifest to activate after successful verification.</param>
        private void PrepareTransaction(ContentManifest remoteManifest)
        {
            if (Directory.Exists(mSettings.TransactionPath))
                throw new InvalidOperationException("Content transaction storage was not cleaned before a new update.");

            Directory.CreateDirectory(GetTransactionPath(DownloadsDirectoryName));
            Directory.CreateDirectory(GetTransactionPath(BackupsDirectoryName));
            Directory.CreateDirectory(GetTransactionPath(CreatedDirectoryName));
            WriteAllTextDurable(GetTransactionPath(ManifestNewFileName),
                                mSerializer.Serialize(remoteManifest));
        }

        /// <summary>
        /// Downloads and verifies every planned bundle before any active file is modified.
        /// </summary>
        /// <param name="plan">The deterministic update plan.</param>
        /// <param name="progress">The optional progress callback.</param>
        /// <param name="cancellationToken">The token used to cancel download and verification.</param>
        /// <returns>A task completed after every staged file is verified.</returns>
        private async Task DownloadAndVerifyAsync(ContentUpdatePlan plan,
                                                  Action<ContentUpdateProgress> progress,
                                                  CancellationToken cancellationToken)
        {
            long completedBytes = 0;
            var downloads       = plan.Downloads;
            for (int i = 0, cnt = downloads.Count; i < cnt; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bundle          = downloads[i];
                var destinationPath = GetTransactionBundlePath(DownloadsDirectoryName, bundle.FileName);
                EnsureParentDirectory(destinationPath);

                long currentBytes = 0;
                await mSource.DownloadBundleAsync(
                    bundle,
                    destinationPath,
                    bytes =>
                    {
                        currentBytes = Math.Max(0, Math.Min(bytes, bundle.Size));
                        Report(progress,
                               EContentUpdateStage.Downloading,
                               bundle.FileName,
                               completedBytes + currentBytes,
                               plan.DownloadBytes,
                               i,
                               downloads.Count);
                    },
                    cancellationToken);

                Report(progress,
                       EContentUpdateStage.Verifying,
                       bundle.FileName,
                       completedBytes + currentBytes,
                       plan.DownloadBytes,
                       i,
                       downloads.Count);
                await VerifyBundleAsync(bundle, destinationPath, cancellationToken);
                completedBytes += bundle.Size;
                Report(progress,
                       EContentUpdateStage.Downloading,
                       bundle.FileName,
                       completedBytes,
                       plan.DownloadBytes,
                       i + 1,
                       downloads.Count);
            }
        }

        /// <summary>
        /// Verifies one staged bundle size and SHA-256 hash.
        /// </summary>
        /// <param name="bundle">The expected remote bundle metadata.</param>
        /// <param name="filePath">The complete staged file path.</param>
        /// <param name="cancellationToken">The token used to cancel hashing.</param>
        /// <returns>A task completed when verification succeeds.</returns>
        private static async Task VerifyBundleAsync(ContentBundle bundle,
                                                    string filePath,
                                                    CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
                throw new InvalidDataException($"Downloaded bundle '{bundle.FileName}' does not exist.");

            var actualSize = new FileInfo(filePath).Length;
            if (actualSize != bundle.Size)
                throw new InvalidDataException($"Downloaded bundle '{bundle.FileName}' size mismatch. Expected {bundle.Size}, received {actualSize}.");

            var actualHash = await ContentHashUtility.ComputeSha256Async(filePath, cancellationToken);
            if (!string.Equals(actualHash, bundle.Hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Downloaded bundle '{bundle.FileName}' SHA-256 mismatch.");
        }

        /// <summary>
        /// Moves verified files into active storage and records a durable commit marker.
        /// </summary>
        /// <param name="plan">The verified update plan to commit.</param>
        private void Commit(ContentUpdatePlan plan)
        {
            var downloads = plan.Downloads;
            for (int i = 0, cnt = downloads.Count; i < cnt; i++)
            {
                var bundle = downloads[i];
                InstallBundle(bundle.FileName);
            }

            var obsoleteFiles = plan.ObsoleteFiles;
            for (int i = 0, cnt = obsoleteFiles.Count; i < cnt; i++)
                BackupBundle(obsoleteFiles[i]);

            InstallManifest();
            WriteAllTextDurable(GetTransactionPath(CommittedMarker), string.Empty);
        }

        /// <summary>
        /// Replaces one active bundle while retaining enough state for rollback.
        /// </summary>
        /// <param name="fileName">The relative bundle file name to install.</param>
        private void InstallBundle(string fileName)
        {
            var stagedPath      = GetTransactionBundlePath(DownloadsDirectoryName, fileName);
            var destinationPath = mSettings.ResolveBundlePath(fileName);
            var backupPath      = GetTransactionBundlePath(BackupsDirectoryName, fileName);
            var createdMarker   = GetTransactionBundlePath(CreatedDirectoryName, fileName + ".marker");

            if (File.Exists(destinationPath))
            {
                EnsureParentDirectory(backupPath);
                File.Move(destinationPath, backupPath);
            }
            else
            {
                EnsureParentDirectory(createdMarker);
                WriteAllTextDurable(createdMarker, string.Empty);
            }

            EnsureParentDirectory(destinationPath);
            File.Move(stagedPath, destinationPath);
        }

        /// <summary>
        /// Moves one obsolete active bundle into transaction backup storage.
        /// </summary>
        /// <param name="fileName">The obsolete relative bundle file name.</param>
        private void BackupBundle(string fileName)
        {
            var destinationPath = mSettings.ResolveBundlePath(fileName);
            if (!File.Exists(destinationPath))
                return;

            var backupPath = GetTransactionBundlePath(BackupsDirectoryName, fileName);
            EnsureParentDirectory(backupPath);
            File.Move(destinationPath, backupPath);
        }

        /// <summary>
        /// Replaces the active manifest after all bundle file changes succeed.
        /// </summary>
        private void InstallManifest()
        {
            var stagedPath  = GetTransactionPath(ManifestNewFileName);
            var backupPath  = GetTransactionPath(ManifestBackupFileName);
            var createdPath = GetTransactionPath(ManifestCreatedMarker);
            if (File.Exists(mSettings.ManifestPath))
                File.Move(mSettings.ManifestPath, backupPath);
            else
                WriteAllTextDurable(createdPath, string.Empty);

            EnsureParentDirectory(mSettings.ManifestPath);
            File.Move(stagedPath, mSettings.ManifestPath);
        }

        /// <summary>
        /// Restores an interrupted uncommitted transaction or cleans a committed transaction.
        /// </summary>
        private void RecoverTransaction()
        {
            if (!Directory.Exists(mSettings.TransactionPath))
                return;
            if (File.Exists(GetTransactionPath(CommittedMarker)))
            {
                Directory.Delete(mSettings.TransactionPath, true);
                return;
            }

            RestoreCreatedBundles();
            RestoreBundleBackups();
            RestoreManifest();
            Directory.Delete(mSettings.TransactionPath, true);
        }

        /// <summary>
        /// Removes active files that did not exist before the interrupted transaction.
        /// </summary>
        private void RestoreCreatedBundles()
        {
            var createdRoot = GetTransactionPath(CreatedDirectoryName);
            if (!Directory.Exists(createdRoot))
                return;

            var markerPaths = Directory.GetFiles(createdRoot, "*.marker", SearchOption.AllDirectories);
            for (int i = 0, cnt = markerPaths.Length; i < cnt; i++)
            {
                var relativeMarker = GetRelativePortablePath(createdRoot, markerPaths[i]);
                var relativeFile   = relativeMarker.Substring(0, relativeMarker.Length - ".marker".Length);
                var destination    = mSettings.ResolveBundlePath(relativeFile);
                if (File.Exists(destination))
                    File.Delete(destination);
            }
        }

        /// <summary>
        /// Restores every active bundle moved into transaction backup storage.
        /// </summary>
        private void RestoreBundleBackups()
        {
            var backupRoot = GetTransactionPath(BackupsDirectoryName);
            if (!Directory.Exists(backupRoot))
                return;

            var backupPaths = Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories);
            for (int i = 0, cnt = backupPaths.Length; i < cnt; i++)
            {
                var backupPath  = backupPaths[i];
                var relativeFile = GetRelativePortablePath(backupRoot, backupPath);
                var destination = mSettings.ResolveBundlePath(relativeFile);
                if (File.Exists(destination))
                    File.Delete(destination);
                EnsureParentDirectory(destination);
                File.Move(backupPath, destination);
            }
        }

        /// <summary>
        /// Restores or removes the active manifest changed by an interrupted transaction.
        /// </summary>
        private void RestoreManifest()
        {
            var backupPath  = GetTransactionPath(ManifestBackupFileName);
            var createdPath = GetTransactionPath(ManifestCreatedMarker);
            if (File.Exists(backupPath))
            {
                if (File.Exists(mSettings.ManifestPath))
                    File.Delete(mSettings.ManifestPath);
                EnsureParentDirectory(mSettings.ManifestPath);
                File.Move(backupPath, mSettings.ManifestPath);
            }
            else if (File.Exists(createdPath) && File.Exists(mSettings.ManifestPath))
                File.Delete(mSettings.ManifestPath);
        }

        /// <summary>
        /// Best-effort removes transaction backups after the durable commit marker exists.
        /// </summary>
        private void TryDeleteCommittedTransaction()
        {
            try
            {
                if (Directory.Exists(mSettings.TransactionPath))
                    Directory.Delete(mSettings.TransactionPath, true);
            }
            catch
            {
                // A committed transaction is safely cleaned during the next update check.
            }
        }

        /// <summary>
        /// Resolves one relative bundle path beneath a private transaction subdirectory.
        /// </summary>
        /// <param name="directoryName">The private transaction subdirectory.</param>
        /// <param name="fileName">The validated manifest bundle file name.</param>
        /// <returns>The normalized private absolute file path.</returns>
        private string GetTransactionBundlePath(string directoryName, string fileName)
        {
            mSettings.ResolveBundlePath(fileName);
            return GetTransactionPath(directoryName + "/" + fileName);
        }

        /// <summary>
        /// Resolves one private relative transaction path without allowing traversal.
        /// </summary>
        /// <param name="relativePath">The private forward-slash relative path.</param>
        /// <returns>The normalized absolute transaction path.</returns>
        private string GetTransactionPath(string relativePath)
        {
            ContentUpdateSettings.ValidateRelativePath(relativePath, nameof(relativePath));
            var platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var fullPath     = Path.GetFullPath(Path.Combine(mSettings.TransactionPath, platformPath));
            var prefix       = mSettings.TransactionPath + Path.DirectorySeparatorChar;
            var comparison   = Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!fullPath.StartsWith(prefix, comparison))
                throw new InvalidOperationException($"Transaction path '{relativePath}' escapes private storage.");
            return fullPath;
        }

        /// <summary>
        /// Gets one portable relative path from a known private root and child file.
        /// </summary>
        /// <param name="rootPath">The complete private root path.</param>
        /// <param name="filePath">The complete child file path.</param>
        /// <returns>A forward-slash relative path.</returns>
        private static string GetRelativePortablePath(string rootPath, string filePath)
        {
            return Path.GetRelativePath(rootPath, filePath)
                       .Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// Creates the parent directory of one file when necessary.
        /// </summary>
        /// <param name="filePath">The complete file path whose parent is required.</param>
        private static void EnsureParentDirectory(string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Writes UTF-8 text and flushes it through the operating-system file buffers.
        /// </summary>
        /// <param name="filePath">The complete destination file path.</param>
        /// <param name="content">The complete text content.</param>
        private static void WriteAllTextDurable(string filePath, string content)
        {
            EnsureParentDirectory(filePath);
            using FileStream stream = new(filePath,
                                          FileMode.Create,
                                          FileAccess.Write,
                                          FileShare.None);
            using StreamWriter writer = new(stream, sEncoding, 1024, true);
            writer.Write(content);
            writer.Flush();
            stream.Flush(true);
        }

        /// <summary>
        /// Invokes the optional progress callback with one immutable value snapshot.
        /// </summary>
        /// <param name="progress">The optional callback.</param>
        /// <param name="stage">The current update stage.</param>
        /// <param name="fileName">The current file name, or null.</param>
        /// <param name="completedBytes">The completed download bytes.</param>
        /// <param name="totalBytes">The total planned download bytes.</param>
        /// <param name="completedFiles">The number of verified files.</param>
        /// <param name="totalFiles">The total number of planned files.</param>
        private static void Report(Action<ContentUpdateProgress> progress,
                                   EContentUpdateStage stage,
                                   string fileName,
                                   long completedBytes,
                                   long totalBytes,
                                   int completedFiles,
                                   int totalFiles)
        {
            if (progress == null)
                return;

            ContentUpdateProgress value = new(stage,
                                              fileName,
                                              completedBytes,
                                              totalBytes,
                                              completedFiles,
                                              totalFiles);
            progress.Invoke(value);
        }
    }
}

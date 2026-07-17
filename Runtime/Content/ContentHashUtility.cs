using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Tritone.Content
{
    /// <summary>
    /// Computes deterministic lowercase SHA-256 hashes without retaining file contents in memory.
    /// </summary>
    public static class ContentHashUtility
    {
        // Defines the reusable streaming read buffer size.
        private const int BufferSize = 64 * 1024;

        // Stores lowercase hexadecimal characters used by hash conversion.
        private static readonly char[] sHexCharacters = "0123456789abcdef".ToCharArray();

        /// <summary>
        /// Computes the lowercase SHA-256 hash of one file using pooled streaming storage.
        /// </summary>
        /// <param name="filePath">The complete file path to hash.</param>
        /// <param name="cancellationToken">The token used to cancel file reading.</param>
        /// <returns>A task containing the 64-character lowercase hexadecimal hash.</returns>
        public static async Task<string> ComputeSha256Async(string filePath,
                                                            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A file path is required.", nameof(filePath));

            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                using FileStream stream = new(filePath,
                                              FileMode.Open,
                                              FileAccess.Read,
                                              FileShare.Read,
                                              BufferSize,
                                              true);
                using SHA256 algorithm = SHA256.Create();
                while (true)
                {
                    var read = await stream.ReadAsync(buffer,
                                                      0,
                                                      buffer.Length,
                                                      cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;

                    algorithm.TransformBlock(buffer, 0, read, buffer, 0);
                }
                algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToLowerHex(algorithm.Hash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Converts one binary hash into lowercase hexadecimal text.
        /// </summary>
        /// <param name="hash">The binary hash bytes.</param>
        /// <returns>The lowercase hexadecimal representation.</returns>
        private static string ToLowerHex(byte[] hash)
        {
            var characters = new char[hash.Length * 2];
            for (int i = 0, cnt = hash.Length; i < cnt; i++)
            {
                var value             = hash[i];
                characters[i * 2]     = sHexCharacters[value >> 4];
                characters[i * 2 + 1] = sHexCharacters[value & 0x0F];
            }
            return new string(characters);
        }
    }
}

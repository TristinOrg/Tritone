using System;
using System.Text;
using Tritone.Saves;
using UnityEngine;

namespace Tritone.Unity.Saves
{
    /// <summary>
    /// Serializes readable UTF-8 JSON saves through Unity JsonUtility.
    /// </summary>
    public sealed class UnityJsonSaveSerializer : ISaveSerializer
    {
        // Stores UTF-8 without a byte-order mark.
        private static readonly Encoding sEncoding = new UTF8Encoding(false);

        /// <inheritdoc />
        public byte[] Serialize<T>(T value) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return sEncoding.GetBytes(JsonUtility.ToJson(value));
        }

        /// <inheritdoc />
        public T Deserialize<T>(byte[] data) where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return JsonUtility.FromJson<T>(sEncoding.GetString(data));
        }
    }
}

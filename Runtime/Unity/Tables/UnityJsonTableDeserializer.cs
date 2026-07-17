using System;
using System.Text;
using Tritone.Tables;
using UnityEngine;

namespace Tritone.Unity.Tables
{
    /// <summary>
    /// Deserializes UTF-8 JSON table assets through Unity's built-in JSON utility.
    /// </summary>
    public sealed class UnityJsonTableDeserializer : ITableDeserializer
    {
        /// <inheritdoc />
        public TRow[] Deserialize<TRow>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var json    = Encoding.UTF8.GetString(data);
            var payload = JsonUtility.FromJson<TablePayload<TRow>>(json);
            return payload?.Rows ?? Array.Empty<TRow>();
        }

        /// <summary>
        /// Provides the object root required by Unity JSON serialization.
        /// </summary>
        [Serializable]
        private sealed class TablePayload<TRow>
        {
            // Stores rows deserialized from the public JSON root.
            public TRow[] Rows;
        }
    }
}

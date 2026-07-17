using System;
using System.Collections.Generic;
using System.Globalization;
using Tritone.Events;
using Tritone.Kernel;
using Tritone.Saves;

namespace Tritone.Settings
{
    /// <summary>
    /// Caches typed settings in memory and persists them through the shared save service.
    /// </summary>
    public sealed class SettingsModule : ModuleBase, ISettingsService
    {
        // Stores the durable settings slot name.
        private readonly string mSlot;

        // Stores serialized invariant values by key.
        private readonly Dictionary<string, string> mValues = new(StringComparer.Ordinal);

        // Stores the shared save service.
        private ISaveService mSaves;

        // Tracks whether memory differs from durable storage.
        private bool mDirty;

        /// <inheritdoc />
        public Event<string> Changed { get; } = new();

        /// <summary>
        /// Initializes settings with one durable save slot.
        /// </summary>
        /// <param name="slot">The save slot used for settings.</param>
        public SettingsModule(string slot = "settings")
        {
            if (string.IsNullOrWhiteSpace(slot))
                throw new ArgumentException(
                    "A settings slot cannot be null, empty, or whitespace.",
                    nameof(slot));
            mSlot = slot;
        }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            mSaves = services.GetRequired<ISaveService>();
            if (mSaves.TryLoad<SettingsData>(mSlot, out var data) && data.Entries != null)
            {
                for (int i = 0, cnt = data.Entries.Length; i < cnt; i++)
                {
                    var entry = data.Entries[i];
                    if (!string.IsNullOrEmpty(entry.Key))
                        mValues[entry.Key] = entry.Value ?? string.Empty;
                }
            }
            services.AddSingleton<ISettingsService>(this);
        }

        /// <inheritdoc />
        public int GetInt(string key, int fallback = 0)
        {
            return mValues.TryGetValue(ValidateKey(key), out var value) &&
                   int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;
        }

        /// <inheritdoc />
        public void SetInt(string key, int value)
        {
            SetValue(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public float GetFloat(string key, float fallback = 0.0f)
        {
            return mValues.TryGetValue(ValidateKey(key), out var value) &&
                   float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;
        }

        /// <inheritdoc />
        public void SetFloat(string key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            SetValue(key, value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public bool GetBool(string key, bool fallback = false)
        {
            return mValues.TryGetValue(ValidateKey(key), out var value) &&
                   bool.TryParse(value, out var result)
                ? result
                : fallback;
        }

        /// <inheritdoc />
        public void SetBool(string key, bool value)
        {
            SetValue(key, value ? bool.TrueString : bool.FalseString);
        }

        /// <inheritdoc />
        public string GetString(string key, string fallback = "")
        {
            return mValues.TryGetValue(ValidateKey(key), out var value)
                ? value
                : fallback;
        }

        /// <inheritdoc />
        public void SetString(string key, string value)
        {
            SetValue(key, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            key = ValidateKey(key);
            if (!mValues.Remove(key))
                return false;

            mDirty = true;
            Changed.Publish(key);
            return true;
        }

        /// <inheritdoc />
        public void Save()
        {
            if (!mDirty)
                return;

            SettingEntry[] entries = new SettingEntry[mValues.Count];
            var index = 0;
            foreach (var pair in mValues)
                entries[index++] = new SettingEntry(pair.Key, pair.Value);
            mSaves.Save(mSlot, new SettingsData(entries));
            mDirty = false;
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            Save();
            Changed.Clear();
            mValues.Clear();
            mSaves = null;
        }

        /// <summary>
        /// Stores one changed invariant value and publishes its key.
        /// </summary>
        private void SetValue(string key, string value)
        {
            key = ValidateKey(key);
            if (mValues.TryGetValue(key, out var current) &&
                string.Equals(current, value, StringComparison.Ordinal))
                return;

            mValues[key] = value;
            mDirty       = true;
            Changed.Publish(key);
        }

        /// <summary>
        /// Validates one non-empty setting key.
        /// </summary>
        private static string ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    "A setting key cannot be null, empty, or whitespace.",
                    nameof(key));
            return key;
        }

        /// <summary>
        /// Stores the serializable settings entry array.
        /// </summary>
        [Serializable]
        private sealed class SettingsData
        {
            // Stores all durable setting entries.
            public SettingEntry[] Entries;

            /// <summary>
            /// Initializes an empty object for deserialization.
            /// </summary>
            public SettingsData() { }

            /// <summary>
            /// Initializes one complete settings snapshot.
            /// </summary>
            public SettingsData(SettingEntry[] entries)
            {
                Entries = entries;
            }
        }

        /// <summary>
        /// Stores one serializable setting key and invariant value.
        /// </summary>
        [Serializable]
        private sealed class SettingEntry
        {
            // Stores the setting key.
            public string Key;

            // Stores the invariant serialized value.
            public string Value;

            /// <summary>
            /// Initializes an empty object for deserialization.
            /// </summary>
            public SettingEntry() { }

            /// <summary>
            /// Initializes one durable setting entry.
            /// </summary>
            public SettingEntry(string key, string value)
            {
                Key   = key;
                Value = value;
            }
        }
    }
}

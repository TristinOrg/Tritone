using Tritone.Events;

namespace Tritone.Settings
{
    /// <summary>
    /// Provides typed application settings with explicit and shutdown persistence.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Gets the event published after one setting changes.
        /// </summary>
        Event<string> Changed { get; }

        /// <summary>
        /// Gets an integer setting or its fallback.
        /// </summary>
        int GetInt(string key, int fallback = 0);

        /// <summary>
        /// Sets an integer setting in memory.
        /// </summary>
        void SetInt(string key, int value);

        /// <summary>
        /// Gets a floating-point setting or its fallback.
        /// </summary>
        float GetFloat(string key, float fallback = 0.0f);

        /// <summary>
        /// Sets a floating-point setting in memory.
        /// </summary>
        void SetFloat(string key, float value);

        /// <summary>
        /// Gets a Boolean setting or its fallback.
        /// </summary>
        bool GetBool(string key, bool fallback = false);

        /// <summary>
        /// Sets a Boolean setting in memory.
        /// </summary>
        void SetBool(string key, bool value);

        /// <summary>
        /// Gets a string setting or its fallback.
        /// </summary>
        string GetString(string key, string fallback = "");

        /// <summary>
        /// Sets a string setting in memory.
        /// </summary>
        void SetString(string key, string value);

        /// <summary>
        /// Removes one setting.
        /// </summary>
        bool Remove(string key);

        /// <summary>
        /// Persists all pending setting changes immediately.
        /// </summary>
        void Save();
    }
}

using System.Threading.Tasks;
using Tritone.Events;

namespace Tritone.Localization
{
    /// <summary>
    /// Provides hot-update-friendly localized text lookup and language switching.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Gets the active language identifier.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the event published after the active language changes.
        /// </summary>
        Event<string> LanguageChanged { get; }

        /// <summary>
        /// Gets localized text or returns its key when missing.
        /// </summary>
        string Get(string key);

        /// <summary>
        /// Formats localized text with supplied arguments.
        /// </summary>
        string Format(string key, params object[] arguments);

        /// <summary>
        /// Loads and activates one language synchronously.
        /// </summary>
        void SetLanguage(string language);

        /// <summary>
        /// Loads and activates one language asynchronously.
        /// </summary>
        Task SetLanguageAsync(string language);
    }
}

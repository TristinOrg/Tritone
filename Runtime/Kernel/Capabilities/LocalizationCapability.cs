using System.Threading.Tasks;
using Tritone.Localization;

namespace Tritone.Kernel
{

    /// <summary>
    /// Provides localization lookup and language switching operations.
    /// </summary>
    public sealed class LocalizationCapability
    {
        // Stores the owning module context.
        private readonly ModuleContext mContext;

        /// <summary>
        /// Initializes localization operations for one module context.
        /// </summary>
        /// <param name="context">The owning module context.</param>
        internal LocalizationCapability(ModuleContext context)
        {
            mContext = context;
        }

        /// <summary>
        /// Gets localized text for one stable key.
        /// </summary>
        /// <param name="key">The stable localization key.</param>
        /// <returns>The active localized text.</returns>
        public string Get(string key)
        {
            return GetService().Get(key);
        }

        /// <summary>
        /// Formats localized text with supplied values.
        /// </summary>
        /// <param name="key">The stable localization key.</param>
        /// <param name="arguments">The format arguments.</param>
        /// <returns>The formatted localized text.</returns>
        public string Format(string key, params object[] arguments)
        {
            return GetService().Format(key, arguments);
        }

        /// <summary>
        /// Loads and activates one language.
        /// </summary>
        /// <param name="language">The target language identifier.</param>
        /// <returns>A task completed after activation.</returns>
        public Task SetLanguageAsync(string language)
        {
            return GetService().SetLanguageAsync(language);
        }

        /// <summary>
        /// Gets the configured localization service.
        /// </summary>
        /// <returns>The application localization service.</returns>
        private ILocalizationService GetService()
        {
            return mContext.GetRequired<ILocalizationService>(
                "Localization is not configured. Call builder.UseLocalization() before adding game modules.");
        }
    }
}

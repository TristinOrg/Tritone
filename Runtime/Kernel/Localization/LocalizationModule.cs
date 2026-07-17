using System;
using System.Globalization;
using System.Threading.Tasks;
using Tritone.Events;
using Tritone.Kernel;
using Tritone.Tables;

namespace Tritone.Localization
{
    /// <summary>
    /// Loads one indexed text table per active language and preserves the previous table on failure.
    /// </summary>
    public sealed class LocalizationModule : ModuleBase, ILocalizationService
    {
        // Stores the initial language loaded during module startup.
        private readonly string mInitialLanguage;

        // Resolves a language identifier to its table asset path.
        private readonly Func<string, string> mPathResolver;

        // Stores the active indexed localization table.
        private Table<string, LocalizationRow> mTable;

        // Invalidates older asynchronous language requests.
        private int mRequestId;

        /// <inheritdoc />
        public string Language { get; private set; }

        /// <inheritdoc />
        public Event<string> LanguageChanged { get; } = new();

        /// <summary>
        /// Initializes localization with an initial language and table path resolver.
        /// </summary>
        /// <param name="initialLanguage">The language loaded during startup.</param>
        /// <param name="pathResolver">The function resolving language table asset paths.</param>
        public LocalizationModule(string initialLanguage, Func<string, string> pathResolver)
        {
            mInitialLanguage = ValidateLanguage(initialLanguage);
            mPathResolver    = pathResolver ??
                               throw new ArgumentNullException(nameof(pathResolver));
        }

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            services.AddSingleton<ILocalizationService>(this);
        }

        /// <inheritdoc />
        protected override void OnStart()
        {
            SetLanguage(mInitialLanguage);
        }

        /// <inheritdoc />
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            return mTable != null && mTable.TryGet(key, out var row)
                ? row.Text ?? string.Empty
                : key;
        }

        /// <inheritdoc />
        public string Format(string key, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 Get(key),
                                 arguments ?? Array.Empty<object>());
        }

        /// <inheritdoc />
        public void SetLanguage(string language)
        {
            language = ValidateLanguage(language);
            if (string.Equals(Language, language, StringComparison.Ordinal))
                return;

            mRequestId++;
            var next = LoadTable<string, LocalizationRow>(ResolvePath(language));
            Activate(language, next);
        }

        /// <inheritdoc />
        public async Task SetLanguageAsync(string language)
        {
            language = ValidateLanguage(language);
            if (string.Equals(Language, language, StringComparison.Ordinal))
                return;

            var requestId = ++mRequestId;
            var next = await LoadTableAsync<string, LocalizationRow>(ResolvePath(language));
            if (requestId != mRequestId)
            {
                ReleaseTable(next);
                return;
            }
            Activate(language, next);
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            mRequestId++;
            if (mTable != null)
                ReleaseTable(ref mTable);
            Language = null;
            LanguageChanged.Clear();
        }

        /// <summary>
        /// Activates one successfully loaded language and releases the previous table.
        /// </summary>
        private void Activate(string language, Table<string, LocalizationRow> table)
        {
            var previous = mTable;
            mTable       = table;
            Language     = language;
            if (previous != null)
                ReleaseTable(previous);
            LanguageChanged.Publish(language);
        }

        /// <summary>
        /// Resolves and validates one language table asset path.
        /// </summary>
        private string ResolvePath(string language)
        {
            var path = mPathResolver.Invoke(language);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException(
                    $"Localization path resolver returned an invalid path for '{language}'.");
            return path;
        }

        /// <summary>
        /// Validates one non-empty language identifier.
        /// </summary>
        private static string ValidateLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException(
                    "A language cannot be null, empty, or whitespace.",
                    nameof(language));
            return language;
        }
    }
}

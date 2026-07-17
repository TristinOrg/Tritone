using System;
using Tritone.Tables;

namespace Tritone.Localization
{
    /// <summary>
    /// Stores one localized text entry loaded through the table system.
    /// </summary>
    [Serializable]
    public sealed class LocalizationRow : ITableRow<string>
    {
        // Stores the stable localization identifier.
        public string Id;

        // Stores the localized text.
        public string Text;

        /// <inheritdoc />
        public string Key => Id;
    }
}

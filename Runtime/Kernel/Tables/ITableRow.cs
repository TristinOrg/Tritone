namespace Tritone.Tables
{
    /// <summary>
    /// Exposes the stable primary key used to index one configuration row.
    /// </summary>
    /// <typeparam name="TKey">The row key type.</typeparam>
    public interface ITableRow<TKey>
    {
        /// <summary>
        /// Gets the stable primary key of this row.
        /// </summary>
        TKey Key { get; }
    }
}

namespace Tritone.Tables
{
    /// <summary>
    /// Creates independently owned configuration table lifetimes.
    /// </summary>
    public interface ITableService
    {
        /// <summary>
        /// Creates one empty table ownership scope.
        /// </summary>
        /// <returns>A new table scope.</returns>
        ITableScope CreateScope();
    }
}

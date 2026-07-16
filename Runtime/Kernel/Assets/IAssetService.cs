namespace Tritone.Assets
{
    /// <summary>
    /// Creates independently owned asset lifetimes for framework consumers.
    /// </summary>
    public interface IAssetService
    {
        /// <summary>
        /// Creates one asset ownership scope.
        /// </summary>
        /// <returns>A new empty asset scope.</returns>
        IAssetScope CreateScope();
    }
}

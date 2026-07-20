namespace Tritone.Tweening
{
    /// <summary>
    /// Creates tween scopes with deterministic owner cleanup.
    /// </summary>
    public interface ITweenService
    {
        /// <summary>
        /// Creates one independently owned tween scope.
        /// </summary>
        /// <returns>A new tween lifetime scope.</returns>
        ITweenScope CreateScope();
    }
}

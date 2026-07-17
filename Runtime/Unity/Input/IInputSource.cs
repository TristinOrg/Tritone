namespace Tritone.Unity.Input
{
    /// <summary>
    /// Abstracts frame input reads for testing and alternative input packages.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>
        /// Gets whether one named button was pressed this frame.
        /// </summary>
        bool GetButtonDown(string action);

        /// <summary>
        /// Gets one named raw axis value.
        /// </summary>
        float GetAxis(string action);
    }
}

namespace Tritone.Unity.Input
{
    /// <summary>
    /// Reads named actions from Unity's built-in input manager.
    /// </summary>
    public sealed class UnityInputSource : IInputSource
    {
        /// <inheritdoc />
        public bool GetButtonDown(string action)
        {
            return UnityEngine.Input.GetButtonDown(action);
        }

        /// <inheritdoc />
        public float GetAxis(string action)
        {
            return UnityEngine.Input.GetAxisRaw(action);
        }
    }
}

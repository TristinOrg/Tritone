namespace Tritone.Networking
{
    /// <summary>
    /// Describes the current transport connection state.
    /// </summary>
    public enum ENetworkState
    {
        Disconnected,
        Connecting,

        /// <summary>
        /// Indicates that transport connection succeeded and application-level validation is running.
        /// </summary>
        Handshaking,
        Connected,
        Disconnecting,
        Reconnecting
    }
}

namespace Tritone.Networking
{
    /// <summary>
    /// Describes the current transport connection state.
    /// </summary>
    public enum ENetworkState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Reconnecting
    }
}

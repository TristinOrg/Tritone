namespace Tritone.Content
{
    /// <summary>
    /// Identifies the current stage of one content update operation.
    /// </summary>
    public enum EContentUpdateStage
    {
        Checking,
        Downloading,
        Verifying,
        Committing,
        Completed
    }
}

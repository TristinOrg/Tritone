namespace Tritone.Kernel
{
    /// <summary>
    /// Defines deterministic execution order shared by all update stages.
    /// </summary>
    public interface IOrderedUpdateSystem
    {
        /// <summary>
        /// Gets the execution order within the implemented update stage.
        /// </summary>
        int Order { get; }
    }
}

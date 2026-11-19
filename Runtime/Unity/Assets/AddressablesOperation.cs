using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tritone.Unity.Assets
{
    /// <summary>
    /// Waits for Addressables operations through their completion event on all platforms.
    /// </summary>
    internal static class AddressablesOperation
    {
        internal static Task<TResult> WaitAsync<TResult>(AsyncOperationHandle<TResult> handle)
        {
            if (handle.IsDone)
                return Task.FromResult(handle.Result);

            TaskCompletionSource<TResult> completion = new();
            handle.Completed += operation => completion.TrySetResult(operation.Result);
            return completion.Task;
        }
    }
}

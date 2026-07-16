using UnityEngine;
using Object = UnityEngine.Object;

namespace Tritone.Unity
{
    /// <summary>
    /// Provides destruction that is safe in both play mode and EditMode tests.
    /// </summary>
    internal static class UnityObjectUtility
    {
        /// <summary>
        /// Destroys a Unity object using the lifecycle required by the current mode.
        /// </summary>
        internal static void Destroy(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }

            Object.DestroyImmediate(target);
        }
    }
}

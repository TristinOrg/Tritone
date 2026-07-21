using System.Collections;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tritone.PlayMode.Tests
{
    /// <summary>
    /// Verifies package bootstrap lifecycle and Unity player-loop integration.
    /// </summary>
    public sealed class BootstrapPlayModeTests
    {
        /// <summary>
        /// Verifies that a MonoBehaviour bootstrap starts and stops one application.
        /// </summary>
        /// <returns>An enumerator advancing the Unity player loop.</returns>
        [UnityTest]
        public IEnumerator Bootstrap_StartsAndStopsApplication()
        {
            var gameObject = new GameObject("Tritone Package Test");
            var bootstrap  = gameObject.AddComponent<PlayModeTestBootstrap>();
            yield return null;

            Assert.IsNotNull(bootstrap);
            Assert.AreEqual(EApplicationState.Running, TritoneBootstrap.Current.State);

            Object.Destroy(gameObject);
            yield return null;
            Assert.IsNull(TritoneBootstrap.Current);
        }

        /// <summary>
        /// Verifies that Unity Update reaches registered Tritone update systems.
        /// </summary>
        /// <returns>An enumerator advancing the Unity player loop.</returns>
        [UnityTest]
        public IEnumerator Bootstrap_ForwardsUnityPlayerLoop()
        {
            var gameObject = new GameObject("Tritone Player Loop Test");
            var bootstrap  = gameObject.AddComponent<PlayModeTestBootstrap>();
            yield return null;

            Assert.Greater(bootstrap.Probe.UpdateCount, 0);

            Object.Destroy(gameObject);
            yield return null;
            Assert.IsNull(TritoneBootstrap.Current);
        }
    }
}

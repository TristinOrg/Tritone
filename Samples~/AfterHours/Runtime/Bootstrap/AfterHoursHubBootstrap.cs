using AfterHours.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AfterHours.Bootstrap
{
    /// <summary>Owns the standalone launcher; feature scenes start their own isolated profiles.</summary>
    public sealed class AfterHoursHubBootstrap : MonoBehaviour
    {
        private AfterHoursHubController mController;

        private void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            mController = new AfterHoursHubController();
            mController.Show();
        }

        private void OnDestroy()
        {
            mController?.Dispose();
            mController = null;
        }
    }
}

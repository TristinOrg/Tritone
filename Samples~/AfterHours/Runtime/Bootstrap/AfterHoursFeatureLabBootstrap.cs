using System;
using AfterHours.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AfterHours.Bootstrap
{
    /// <summary>Starts one focused, self-guided feature lab with a direct route back to the Sample Hub.</summary>
    public sealed class AfterHoursFeatureLabBootstrap : MonoBehaviour
    {
        [SerializeField] private string mLabId;
        private AfterHoursFeatureLabController mController;
        public string LabId { get => mLabId; set => mLabId = value; }

        private void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            mController = new AfterHoursFeatureLabController(mLabId);
            mController.Show();
        }

        private void Update() => mController?.Tick();

        private void OnDestroy()
        {
            mController?.Dispose();
            mController = null;
        }
    }
}

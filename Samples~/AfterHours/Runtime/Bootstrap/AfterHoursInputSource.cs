using Tritone.Unity.Input;
using UnityEngine;

namespace AfterHours.Bootstrap
{
    /// <summary>Maps familiar keyboard controls onto Tritone's named input scope.</summary>
    public sealed class AfterHoursInputSource : IInputSource
    {
        public bool GetButtonDown(string action)
        {
            switch (action)
            {
                case "Interact": return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
                case "Inspect": return Input.GetKeyDown(KeyCode.Q);
                case "OpenManual": return Input.GetKeyDown(KeyCode.Tab);
                case "BasicStrike": return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha1);
                case "Pause": return Input.GetKeyDown(KeyCode.Escape);
                default: return false;
            }
        }

        public float GetAxis(string action)
        {
            if (action != "Horizontal")
                return 0f;
            float value = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) value -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) value += 1f;
            return value;
        }
    }
}

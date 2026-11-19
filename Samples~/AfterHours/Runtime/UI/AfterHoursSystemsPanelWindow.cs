using System;
using AfterHours.Generated.UI;
using Tritone.Unity.UI;

namespace AfterHours.UI
{
    /// <summary>A Tritone-managed popup backed by a generator-authored typed prefab view.</summary>
    public sealed class AfterHoursSystemsPanelWindow : UIWindow<SystemsPanelView>
    {
        public event Action Closed;

        protected override void OnBindEvents()
        {
            BindButton(mView.CloseButton, Close);
        }

        protected override void OnOpen()
        {
            mView.BodyText.text =
                "TRITONE POPUP\n\n" +
                "This window renders above the guide on the Popup layer.\n\n" +
                "Click CLOSE to exercise the generated button binding.";
        }

        protected override void OnClose() => Closed?.Invoke();
    }
}

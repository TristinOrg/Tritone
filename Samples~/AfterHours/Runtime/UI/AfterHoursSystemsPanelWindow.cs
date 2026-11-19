using AfterHours.Generated.UI;
using Tritone.Unity.UI;

namespace AfterHours.UI
{
    /// <summary>A Tritone-managed popup backed by a generator-authored typed prefab view.</summary>
    public sealed class AfterHoursSystemsPanelWindow : UIWindow<SystemsPanelView>
    {
        protected override void OnBindEvents()
        {
            BindButton(mView.CloseButton, Close);
        }

        protected override void OnOpen()
        {
            mView.BodyText.text =
                "WINDOW LIFETIME / owned by the active game module\n\n" +
                "VIEW BINDING / generated field references\n\n" +
                "SORTING / assigned to Tritone's popup layer";
        }
    }
}

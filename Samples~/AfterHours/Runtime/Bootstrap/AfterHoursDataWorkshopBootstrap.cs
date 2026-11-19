using AfterHours.Generated.Tables;
using AfterHours.UI;
using Tritone.Kernel;
using Tritone.Tables;
using Tritone.Unity;
using Tritone.Unity.Assets;
using Tritone.Unity.Tables;
using UnityEngine.EventSystems;

namespace AfterHours.Bootstrap
{
    /// <summary>Loads a typed table through Tritone without starting the RPG.</summary>
    public sealed class AfterHoursDataWorkshopBootstrap : TritoneBootstrap
    {
        private AfterHoursFeatureLabController mController;

        protected override void Configure(GameApplicationBuilder builder) => builder.UseAssets().UseTables();

        protected override void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new UnityEngine.GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            base.Awake();
            mController = new AfterHoursFeatureLabController("DataWorkshop", LoadTable, ResetResult);
            mController.Show();
        }

        private void LoadTable()
        {
            using (var tables = Application.Services.GetRequired<ITableService>().CreateScope())
            {
                var clues = tables.Load<string, ClueRow>(ClueTable.Path);
                var first = clues.Count > 0 ? clues.GetAt(0) : null;
                mController.SetVisualCount(clues.Count);
                string value = first == null
                    ? $"TABLE LOADED / {clues.Count} rows / no sample row found."
                    : $"TABLE LOADED / {clues.Count} rows\nFIRST ROW / {first.Id} — {first.Title}";
                mController.SetResult(value);
            }
        }

        private void ResetResult() => mController?.ResetResult();

        protected override void OnDestroy()
        {
            mController?.Dispose();
            mController = null;
            base.OnDestroy();
        }
    }
}

using System;
using AfterHours.UI;
using Tritone.Assets;
using Tritone.Kernel;
using Tritone.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AfterHours.Bootstrap
{
    /// <summary>Shared one-action shell for optional provider profiles.</summary>
    public abstract class AfterHoursOptionalProfileBootstrap : TritoneBootstrap
    {
        private AfterHoursFeatureLabController mController;
        private IAssetScope mAssets;

        protected abstract string LabId { get; }
        protected abstract void ConfigureProfile(GameApplicationBuilder builder);

        protected override void Configure(GameApplicationBuilder builder) => ConfigureProfile(builder);

        protected override void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("After Hours Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            base.Awake();
            mAssets = Application.Services.GetRequired<IAssetService>().CreateScope();
            mController = new AfterHoursFeatureLabController(LabId, RunProbe, ResetProbe);
            mController.Show();
        }

        protected virtual async void RunProbe()
        {
            try
            {
                var asset = await mAssets.LoadAsync<TextAsset>("Tables/clues");
                SetVisualValue($"{asset.name}\n{asset.text.Length} characters");
                mController?.SetResult($"ASSET LOADED / {asset.name} / {asset.text.Length} characters through the active provider.");
            }
            catch (Exception exception)
            {
                mController?.SetResult("ASSET LOAD FAILED / Build this provider's sample content first.\n" + exception.Message);
            }
        }

        protected virtual void ResetProbe()
        {
            mAssets?.Dispose();
            mAssets = Application.Services.GetRequired<IAssetService>().CreateScope();
            mController?.ResetResult();
            mController?.SetResult("READY / Build this profile's sample content, then click Load Asset.");
        }

        protected void SetResult(string value) => mController?.SetResult(value);
        protected void SetVisualValue(string value) => mController?.SetVisualValue(value);
        protected void SetVisualStage(int stage) => mController?.SetStage(stage);
        protected void SetVisualProgress(float progress) => mController?.SetProgress(progress);
        protected void ResetVisual() => mController?.ResetResult();

        protected override void OnDestroy()
        {
            mController?.Dispose();
            mAssets?.Dispose();
            mController = null;
            mAssets = null;
            base.OnDestroy();
        }
    }
}

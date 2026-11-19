using System.IO;
using Tritone.Kernel;
using Tritone.Unity.Assets;
using Tritone.Unity.Assets.AssetBundles;

namespace AfterHours.Bootstrap
{
    /// <summary>Optional profile backed by a generated local AssetBundle manifest.</summary>
    public sealed class AfterHoursAssetBundleBootstrap : AfterHoursOptionalProfileBootstrap
    {
        protected override string LabId => "BundleDesk";

        protected override void ConfigureProfile(GameApplicationBuilder builder)
        {
            const string bundleName = "after-hours-case-data";
            var registry = new AssetBundleRegistry()
                .AddBundle(bundleName, bundleName)
                .AddAsset("Tables/clues", bundleName, "Assets/AfterHours/Runtime/Resources/Tables/clues.json");
            string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string contentRoot = Path.Combine(projectRoot, "ContentBuilds", "Current");
            builder.UseAssets(new AssetBundleAssetProvider(registry, new FileAssetBundleSource(contentRoot)));
        }
    }
}

using Tritone.Kernel;
using Tritone.Unity.Assets;

namespace AfterHours.Bootstrap
{
    /// <summary>Optional profile backed by Unity Addressables instead of Resources.</summary>
    public sealed class AfterHoursAddressablesBootstrap : AfterHoursOptionalProfileBootstrap
    {
        protected override string LabId => "AddressablesDesk";
        protected override void ConfigureProfile(GameApplicationBuilder builder) => builder.UseAddressableAssets();
    }
}

using System;
using System.IO;
using Tritone.Kernel;
using Tritone.Unity.ContentUpdates;

namespace AfterHours.Bootstrap
{
    /// <summary>Optional Resources profile with transactional content updates enabled.</summary>
    public sealed class AfterHoursContentBootstrap : AfterHoursOptionalProfileBootstrap
    {
        protected override string LabId => "ContentWorkshop";

        protected override void ConfigureProfile(GameApplicationBuilder builder)
        {
            string contentRoot = Path.Combine(UnityEngine.Application.persistentDataPath, "AfterHoursContent");
            var options = new ContentUpdateOptions(
                "http://127.0.0.1:27832/manifest.json", "http://127.0.0.1:27832/", contentRoot,
                "manifest.json", retryCount: 1, requestTimeoutSeconds: 5);
            builder.UseContentAssets(options);
        }

        protected override async void RunProbe()
        {
            using (var scope = Application.Services.GetRequired<Tritone.Content.IContentUpdateService>().CreateScope())
            {
                try
                {
                    SetResult("CHECKING / contacting the optional local content host...");
                    var result = await scope.UpdateAsync(progress =>
                        SetResult($"CONTENT UPDATE / {progress.Stage} / {progress.CompletedBytes} / {progress.TotalBytes} bytes"));
                    SetResult($"UPDATE COMPLETE / {result.ActiveManifest?.Version ?? "active content"}");
                }
                catch (Exception exception)
                {
                    SetResult("UPDATE UNAVAILABLE / Start the optional local content host.\n" + exception.Message);
                }
            }
        }

        protected override void ResetProbe() => SetResult("READY / Start the optional local content host, then click Check Update.");
    }
}

using AfterHours.Entities;
using AfterHours.Modules;
using Tritone.Kernel;
using Tritone.Unity;
using UnityEngine;

namespace AfterHours.Bootstrap
{
    /// <summary>Runs the ECS lab on a real scene-owned Tritone world.</summary>
    public sealed class AfterHoursEcsLabBootstrap : TritoneBootstrap
    {
        public static void LeaveLab()
        {
            var instance = FindObjectOfType<AfterHoursEcsLabBootstrap>();
            if (instance != null)
                Destroy(instance.gameObject);
        }

        protected override void Configure(GameApplicationBuilder builder)
        {
            builder.UseEntities(64)
                   .AddSceneComponent<EvidenceMarkerComponent>()
                   .AddSceneComponent<CombatActorComponent>()
                   .AddSceneEntitySystem<SignalPresentationSystem>()
                   .AddSceneModule<AfterHoursEcsLabSceneModule>()
                   .UseInitialSceneModule<AfterHoursEcsLabSceneModule>();
        }
    }
}

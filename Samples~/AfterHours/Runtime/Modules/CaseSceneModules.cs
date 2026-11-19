using AfterHours.Entities;
using AfterHours.Models;
using AfterHours.Domain;
using Tritone.Kernel;
using Tritone.Timing;
using Tritone.Tweening;

namespace AfterHours.Modules
{
    public sealed class OfficeSceneModule : ModuleBase
    {
        protected override void OnStart()
        {
            Context.Events.Bind(Context.Services.GetRequired<AfterHoursEvents>().NightTick,
                                () => Logger.Debug("Night watch tick."));
            Context.Tweens.Play(0f, 1f, 1.6d, _ => { }, ETweenEase.InOutQuad,
                                ETimerTimeMode.Unscaled);
            Context.Pools.Rent<ReusableCaseNote>();
        }
    }

    public sealed class CaseSceneModule : ModuleBase, Tritone.Kernel.IUpdateSystem
    {
        private float mElapsed;
        public int Order => 20;

        protected override void OnStart()
        {
            Context.Models.Get<CaseSessionModel>();
            var world = Context.Entities.Scene;
            for (int i = 0; i < 4; i++)
            {
                var entity = world.Create();
                world.Add(entity, new EvidenceMarkerComponent
                {
                    LocationIndex = i,
                    HasEvidence = i == 1 || i == 3,
                    Pulse = i * 0.17f
                });
            }

            var antagonist = world.Create();
            world.Add(antagonist, new CombatActorComponent
            {
                ActorId = 90,
                Health = 26,
                MaximumHealth = 26,
                IsEnemy = true,
                IntentSeconds = 0.4f
            });
        }

        public void Update(in FrameTime time)
        {
            mElapsed += (float)time.DeltaTime;
        }

        protected override void OnStop() => mElapsed = 0f;
    }

    public sealed class SystemsLabSceneModule : ModuleBase
    {
        protected override void OnStart()
        {
            Context.Services.GetRequired<AfterHoursEvents>()
                   .TranscriptAdded.Publish("SYSTEMS LAB / LOCAL SERVICES ARE OPT-IN");
        }
    }

    public sealed class ReusableCaseNote
    {
        public string Text;
        public void Reset() => Text = null;
    }
}

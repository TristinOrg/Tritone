using Tritone.Entities;
using Tritone.Kernel;

namespace AfterHours.Entities
{
    /// <summary>Animates evidence pulses and advances visible enemy intent cues.</summary>
    public sealed class SignalPresentationSystem : IEntitySystem
    {
        private EntityWorld mWorld;
        public int Order => 20;

        public void Initialize(EntityWorld world) => mWorld = world;

        public void Update(in FrameTime time)
        {
            if (mWorld == null)
                return;
            var markers = mWorld.Query<EvidenceMarkerComponent>();
            for (int i = 0; i < markers.Count; i++)
            {
                ref var marker = ref markers.GetComponent(i);
                marker.Pulse += (float)time.UnscaledDeltaTime * 0.8f;
                if (marker.Pulse >= 1f)
                    marker.Pulse -= 1f;
            }

            var actors = mWorld.Query<CombatActorComponent>();
            for (int i = 0; i < actors.Count; i++)
            {
                ref var actor = ref actors.GetComponent(i);
                if (actor.IsEnemy && actor.Health > 0)
                    actor.IntentSeconds = (float)((actor.IntentSeconds + time.DeltaTime) % 2.4d);
            }
        }

        public void Shutdown() => mWorld = null;
    }
}

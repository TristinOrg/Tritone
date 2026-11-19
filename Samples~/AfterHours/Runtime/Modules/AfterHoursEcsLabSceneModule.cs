using AfterHours.Entities;
using Tritone.Entities;
using Tritone.Kernel;

namespace AfterHours.Modules
{
    /// <summary>Seeds and owns the ECS lab's scene world for the duration of the lab.</summary>
    public sealed class AfterHoursEcsLabSceneModule : ModuleBase
    {
        protected override void OnStart()
        {
            AfterHoursEcsLabWorld.Bind(Context.Entities.Scene);
            AfterHoursEcsLabWorld.Spawn();
            AfterHoursEcsLabWorld.Spawn();
        }

        protected override void OnStop() => AfterHoursEcsLabWorld.Unbind();
    }

    /// <summary>Small interactive bridge used by the ECS lab's UI.</summary>
    public static class AfterHoursEcsLabWorld
    {
        private static EntityWorld sWorld;
        public static bool IsReady => sWorld != null;

        internal static void Bind(EntityWorld world) => sWorld = world;
        internal static void Unbind() => sWorld = null;

        public static int Count
        {
            get
            {
                if (sWorld == null) return 0;
                var query = sWorld.Query<EvidenceMarkerComponent>();
                return query.Count;
            }
        }

        public static float FirstPulse
        {
            get
            {
                if (sWorld == null) return 0f;
                var query = sWorld.Query<EvidenceMarkerComponent>();
                if (query.Count == 0) return 0f;
                return query.GetComponent(0).Pulse;
            }
        }

        public static void Spawn()
        {
            if (sWorld == null) return;
            var entity = sWorld.Create();
            sWorld.Add(entity, new EvidenceMarkerComponent
            {
                LocationIndex = Count,
                HasEvidence = Count % 2 == 0,
                Pulse = 0f
            });
        }

        public static void Reset()
        {
            if (sWorld == null) return;
            var query = sWorld.Query<EvidenceMarkerComponent>();
            for (int i = query.Count - 1; i >= 0; i--)
                sWorld.Destroy(query.GetEntity(i));
        }
    }
}

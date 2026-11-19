using Tritone.Entities;

namespace AfterHours.Entities
{
    public struct EvidenceMarkerComponent : IEntityComponent
    {
        public int LocationIndex;
        public bool HasEvidence;
        public float Pulse;
    }

    public struct CombatActorComponent : IEntityComponent
    {
        public int ActorId;
        public int Health;
        public int MaximumHealth;
        public int Focus;
        public bool IsEnemy;
        public float IntentSeconds;
    }
}

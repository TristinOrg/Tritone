namespace AfterHours.Domain
{
    /// <summary>Outcome data for a successful encounter action.</summary>
    public readonly struct EncounterActionResult
    {
        public EncounterActionResult(int actorId, int targetId, string skillId, int damage, bool targetDefeated)
        {
            ActorId = actorId;
            TargetId = targetId;
            SkillId = skillId;
            Damage = damage;
            TargetDefeated = targetDefeated;
        }

        public int ActorId { get; }
        public int TargetId { get; }
        public string SkillId { get; }
        public int Damage { get; }
        public bool TargetDefeated { get; }
    }
}

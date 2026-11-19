using System;

namespace AfterHours.Domain
{
    /// <summary>Immutable definition compiled from the sample skill table.</summary>
    public sealed class EncounterSkill
    {
        public EncounterSkill(string id, int focusCost, int power, int stunTurns = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A skill needs a stable id.", nameof(id));
            if (focusCost < 0 || power < 0 || stunTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(focusCost));

            Id = id;
            FocusCost = focusCost;
            Power = power;
            StunTurns = stunTurns;
        }

        public string Id { get; }
        public int FocusCost { get; }
        public int Power { get; }
        public int StunTurns { get; }
    }
}

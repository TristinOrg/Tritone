using System;

namespace AfterHours.Domain
{
    /// <summary>Mutable combat values for one participant; owned by one encounter.</summary>
    public sealed class EncounterActor
    {
        public EncounterActor(int id,
                              string name,
                              bool isEnemy,
                              int maxHealth,
                              int maxFocus,
                              int attack,
                              int defense,
                              int initiative,
                              int stunTurns = 0)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("An encounter actor needs a name.", nameof(name));
            if (maxHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (maxFocus < 0 || attack < 0 || defense < 0)
                throw new ArgumentOutOfRangeException(nameof(maxFocus));
            if (stunTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(stunTurns));

            Id = id;
            Name = name;
            IsEnemy = isEnemy;
            MaxHealth = maxHealth;
            Health = maxHealth;
            MaxFocus = maxFocus;
            Focus = maxFocus;
            Attack = attack;
            Defense = defense;
            Initiative = initiative;
            StunTurns = stunTurns;
        }

        public int Id { get; }
        public string Name { get; }
        public bool IsEnemy { get; }
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public int MaxFocus { get; }
        public int Focus { get; private set; }
        public int Attack { get; }
        public int Defense { get; }
        public int Initiative { get; }
        public int StunTurns { get; internal set; }
        public bool IsAlive => Health > 0;

        internal void SpendFocus(int amount)
        {
            Focus -= amount;
        }

        internal int TakeDamage(int amount)
        {
            int applied = Math.Min(Health, Math.Max(0, amount));
            Health -= applied;
            return applied;
        }
    }
}

using System;
using System.Collections.Generic;

namespace AfterHours.Domain
{
    /// <summary>Owns initiative order and mutable actors for one turn-based encounter.</summary>
    public sealed class EncounterState
    {
        private readonly List<EncounterActor> mActors;
        private int mCurrentIndex = -1;

        public EncounterState(IList<EncounterActor> actors)
        {
            if (actors == null)
                throw new ArgumentNullException(nameof(actors));
            if (actors.Count == 0)
                throw new ArgumentException("An encounter needs participants.", nameof(actors));

            mActors = new List<EncounterActor>(actors.Count);
            var ids = new HashSet<int>();
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i] ?? throw new ArgumentException("Participants cannot be null.", nameof(actors));
                if (!ids.Add(actor.Id))
                    throw new ArgumentException($"Duplicate encounter actor id '{actor.Id}'.", nameof(actors));
                mActors.Add(actor);
            }
            mActors.Sort((left, right) =>
            {
                int initiative = right.Initiative.CompareTo(left.Initiative);
                return initiative != 0 ? initiative : left.Id.CompareTo(right.Id);
            });
            AdvanceTurn();
        }

        public IReadOnlyList<EncounterActor> Actors => mActors;
        public int Round { get; private set; } = 1;
        public bool IsComplete { get; private set; }
        public bool PartyWon { get; private set; }

        public EncounterActor CurrentActor =>
            mCurrentIndex >= 0 && mCurrentIndex < mActors.Count ? mActors[mCurrentIndex] : null;

        public EncounterActor FindActor(int id)
        {
            for (int i = 0; i < mActors.Count; i++)
                if (mActors[i].Id == id)
                    return mActors[i];
            return null;
        }

        public void AdvanceTurn()
        {
            if (IsComplete)
                return;

            if (EvaluateVictory())
                return;

            int previous = mCurrentIndex;
            for (int offset = 1; offset <= mActors.Count; offset++)
            {
                int index = (previous + offset + mActors.Count) % mActors.Count;
                var actor = mActors[index];
                if (!actor.IsAlive)
                    continue;
                if (actor.StunTurns > 0)
                {
                    actor.StunTurns--;
                    continue;
                }

                if (previous >= 0 && index <= previous)
                    Round++;
                mCurrentIndex = index;
                return;
            }

            EvaluateVictory();
            mCurrentIndex = -1;
        }

        public void CompleteIfResolved()
        {
            EvaluateVictory();
        }

        private bool EvaluateVictory()
        {
            bool partyAlive = false;
            bool enemiesAlive = false;
            for (int i = 0; i < mActors.Count; i++)
            {
                if (!mActors[i].IsAlive)
                    continue;
                if (mActors[i].IsEnemy)
                    enemiesAlive = true;
                else
                    partyAlive = true;
            }

            if (!partyAlive || !enemiesAlive)
            {
                IsComplete = true;
                PartyWon = partyAlive && !enemiesAlive;
            }
            return IsComplete;
        }
    }
}

using System;
using System.Collections.Generic;

namespace AfterHours.Domain
{
    /// <summary>Applies validated actions to a single encounter.</summary>
    public sealed class EncounterRules
    {
        private readonly Dictionary<string, EncounterSkill> mSkills;

        public EncounterRules(IEnumerable<EncounterSkill> skills)
        {
            if (skills == null)
                throw new ArgumentNullException(nameof(skills));
            mSkills = new Dictionary<string, EncounterSkill>(StringComparer.Ordinal);
            foreach (var skill in skills)
            {
                if (skill == null)
                    throw new ArgumentException("Skill definitions cannot be null.", nameof(skills));
                if (mSkills.ContainsKey(skill.Id))
                    throw new ArgumentException($"Duplicate skill id '{skill.Id}'.", nameof(skills));
                mSkills.Add(skill.Id, skill);
            }
        }

        public bool TryUseSkill(EncounterState state,
                                int actorId,
                                int targetId,
                                string skillId,
                                out EncounterActionResult result)
        {
            result = default;
            if (state == null || state.IsComplete || state.CurrentActor == null ||
                state.CurrentActor.Id != actorId ||
                !mSkills.TryGetValue(skillId ?? string.Empty, out var skill))
                return false;

            var actor = state.FindActor(actorId);
            var target = state.FindActor(targetId);
            if (actor == null || target == null || !actor.IsAlive || !target.IsAlive ||
                actor.IsEnemy == target.IsEnemy || actor.Focus < skill.FocusCost)
                return false;

            actor.SpendFocus(skill.FocusCost);
            int damage = target.TakeDamage(Math.Max(1, skill.Power + actor.Attack - target.Defense));
            if (skill.StunTurns > 0 && target.IsAlive)
                target.StunTurns = Math.Max(target.StunTurns, skill.StunTurns);

            result = new EncounterActionResult(actor.Id,
                                              target.Id,
                                              skill.Id,
                                              damage,
                                              !target.IsAlive);
            state.CompleteIfResolved();
            if (!state.IsComplete)
                state.AdvanceTurn();
            return true;
        }
    }
}

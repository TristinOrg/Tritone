using System;

namespace AfterHours.Domain
{
    public enum CaseOutcome
    {
        Resolved,
        Withdrawn,
        Failed
    }

    /// <summary>Tracks one authored case and grants its resolution reward once.</summary>
    public sealed class AfterHoursCase
    {
        public AfterHoursCase(string caseId, int experienceReward)
        {
            if (string.IsNullOrWhiteSpace(caseId))
                throw new ArgumentException("A case needs a stable id.", nameof(caseId));
            if (experienceReward < 0)
                throw new ArgumentOutOfRangeException(nameof(experienceReward));
            CaseId = caseId;
            ExperienceReward = experienceReward;
        }

        public string CaseId { get; }
        public int ExperienceReward { get; }
        public bool IsResolved { get; private set; }
        public bool RewardGranted { get; private set; }
        public int ExperienceAwarded { get; private set; }
        public CaseOutcome? Outcome { get; private set; }

        public bool Resolve(CaseOutcome outcome)
        {
            if (IsResolved)
                return false;

            IsResolved = true;
            Outcome = outcome;
            RewardGranted = outcome == CaseOutcome.Resolved;
            ExperienceAwarded = RewardGranted ? ExperienceReward : 0;
            return true;
        }
    }
}

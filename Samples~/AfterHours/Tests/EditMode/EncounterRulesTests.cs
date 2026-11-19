using System.Collections.Generic;
using AfterHours.Domain;
using NUnit.Framework;

namespace AfterHours.Tests
{
    public sealed class EncounterRulesTests
    {
        [Test]
        public void EncounterSkipsUnavailableActors()
        {
            var stunned = new EncounterActor(1, "Morrow", false, 20, 4, 5, 0, 1, stunTurns: 1);
            var enemy = new EncounterActor(2, "Platform Echo", true, 20, 0, 3, 0, 1);
            var state = new EncounterState(new List<EncounterActor> { stunned, enemy });

            Assert.That(state.CurrentActor.Id, Is.EqualTo(2));
            state.AdvanceTurn();
            Assert.That(state.CurrentActor.Id, Is.EqualTo(1));
            Assert.That(stunned.StunTurns, Is.Zero);
        }

        [Test]
        public void SkillCostAndDamageAreApplied()
        {
            var actor = new EncounterActor(1, "Morrow", false, 20, 8, 7, 2, 3);
            var target = new EncounterActor(2, "Platform Echo", true, 30, 0, 4, 1, 2);
            var state = new EncounterState(new List<EncounterActor> { actor, target });
            var rules = new EncounterRules(new[] { new EncounterSkill("counter-signal", 3, 8) });

            var used = rules.TryUseSkill(state, 1, 2, "counter-signal", out var result);

            Assert.That(used, Is.True);
            Assert.That(actor.Focus, Is.EqualTo(5));
            Assert.That(target.Health, Is.EqualTo(16));
            Assert.That(result.Damage, Is.EqualTo(14));
            Assert.That(state.CurrentActor.Id, Is.EqualTo(2));
        }

        [Test]
        public void MissionResolutionIsIdempotent()
        {
            var mission = new AfterHoursCase("last-stop", 40);

            Assert.That(mission.Resolve(CaseOutcome.Resolved), Is.True);
            Assert.That(mission.Resolve(CaseOutcome.Resolved), Is.False);

            Assert.That(mission.RewardGranted, Is.True);
            Assert.That(mission.ExperienceAwarded, Is.EqualTo(40));
        }

        [Test]
        public void FailedCaseDoesNotGrantRewards()
        {
            var mission = new AfterHoursCase("last-stop", 40);

            mission.Resolve(CaseOutcome.Withdrawn);

            Assert.That(mission.RewardGranted, Is.False);
            Assert.That(mission.ExperienceAwarded, Is.Zero);
        }
    }
}

using System;
using System.Collections.Generic;
using AfterHours.Domain;
using Tritone.Models;

namespace AfterHours.Models
{
    /// <summary>Scene-lifetime state for the currently active case.</summary>
    public sealed class CaseSessionModel : IModel
    {
        private readonly HashSet<string> mEvidence = new(StringComparer.Ordinal);

        public string CaseId { get; private set; }
        public int SelectedLocation { get; private set; }
        public int SignalReserve { get; private set; }
        public int ClueCount => mEvidence.Count;
        public bool EncounterStarted { get; private set; }
        public bool IsResolved { get; private set; }
        public CaseOutcome? Outcome { get; private set; }

        public void Initialize() => Begin("last-stop", 3);

        public void Begin(string caseId, int signalReserve)
        {
            if (string.IsNullOrWhiteSpace(caseId))
                throw new ArgumentException("A case id is required.", nameof(caseId));
            CaseId = caseId;
            SignalReserve = Math.Max(0, signalReserve);
            SelectedLocation = 0;
            EncounterStarted = false;
            IsResolved = false;
            Outcome = null;
            mEvidence.Clear();
        }

        public void SelectLocation(int index)
        {
            SelectedLocation = Math.Max(0, Math.Min(3, index));
        }

        public bool RecordEvidence(string evidenceId)
        {
            if (IsResolved || string.IsNullOrWhiteSpace(evidenceId))
                return false;
            return mEvidence.Add(evidenceId);
        }

        public bool SpendSignal(int amount)
        {
            if (amount < 0 || amount > SignalReserve)
                return false;
            SignalReserve -= amount;
            return true;
        }

        public bool StartEncounter()
        {
            if (IsResolved || EncounterStarted || ClueCount < 2)
                return false;
            EncounterStarted = true;
            return true;
        }

        public bool Resolve(CaseOutcome outcome)
        {
            if (IsResolved)
                return false;
            IsResolved = true;
            Outcome = outcome;
            return true;
        }

        public void Reset()
        {
            CaseId = null;
            SignalReserve = 0;
            SelectedLocation = 0;
            EncounterStarted = false;
            IsResolved = false;
            Outcome = null;
            mEvidence.Clear();
        }

        public void Dispose() => Reset();
    }
}

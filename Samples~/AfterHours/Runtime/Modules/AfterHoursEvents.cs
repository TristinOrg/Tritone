using Tritone.Events;
using AfterHours.Domain;

namespace AfterHours.Modules
{
    /// <summary>Typed events shared by game modules and scene-owned screens.</summary>
    public sealed class AfterHoursEvents
    {
        public Event<string> EvidenceRecorded { get; } = new();
        public Event<CaseOutcome> CaseResolved { get; } = new();
        public Event<int> TurnAdvanced { get; } = new();
        public Event<string> TranscriptAdded { get; } = new();
        public Event<string> ScreenChanged { get; } = new();
        public Event<int> SignalChanged { get; } = new();
        public Event NightTick { get; } = new();
    }
}

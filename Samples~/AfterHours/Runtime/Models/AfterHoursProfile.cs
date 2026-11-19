using System;
using System.Collections.Generic;
using Tritone.Models;

namespace AfterHours.Models
{
    /// <summary>Long-lived, save-friendly player progression.</summary>
    [Serializable]
    public sealed class AfterHoursProfile : IModel
    {
        public int SchemaVersion = 1;
        public string Callsign = "Morrow";
        public int Rank = 1;
        public int Experience;
        public int SignalShards = 2;
        public string Language = "en-US";
        public List<string> ResolvedCases = new();
        public List<string> ArchiveEntries = new();

        public void Initialize() => ProfileDefaults.Normalize(this);

        public void Reset()
        {
            SchemaVersion = 1;
            Callsign = "Morrow";
            Rank = 1;
            Experience = 0;
            SignalShards = 2;
            Language = "en-US";
            ResolvedCases?.Clear();
            ArchiveEntries?.Clear();
            ProfileDefaults.Normalize(this);
        }

        public void Dispose() { }

        public void CopyFrom(AfterHoursProfile source)
        {
            if (source == null)
                return;
            SchemaVersion = source.SchemaVersion;
            Callsign = source.Callsign;
            Rank = source.Rank;
            Experience = source.Experience;
            SignalShards = source.SignalShards;
            Language = source.Language;
            ResolvedCases = source.ResolvedCases == null
                ? new List<string>()
                : new List<string>(source.ResolvedCases);
            ArchiveEntries = source.ArchiveEntries == null
                ? new List<string>()
                : new List<string>(source.ArchiveEntries);
            ProfileDefaults.Normalize(this);
        }
    }

    /// <summary>Repairs absent or malformed saved values without blocking a new game.</summary>
    public static class ProfileDefaults
    {
        public static AfterHoursProfile Normalize(AfterHoursProfile profile)
        {
            profile ??= new AfterHoursProfile();
            profile.SchemaVersion = Math.Max(1, profile.SchemaVersion);
            if (string.IsNullOrWhiteSpace(profile.Callsign))
                profile.Callsign = "Morrow";
            profile.Rank = Math.Max(1, profile.Rank);
            profile.Experience = Math.Max(0, profile.Experience);
            profile.SignalShards = Math.Max(0, profile.SignalShards);
            if (string.IsNullOrWhiteSpace(profile.Language))
                profile.Language = "en-US";
            profile.ResolvedCases ??= new List<string>();
            profile.ArchiveEntries ??= new List<string>();
            return profile;
        }
    }
}

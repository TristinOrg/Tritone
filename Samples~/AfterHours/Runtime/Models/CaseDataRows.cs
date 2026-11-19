using Tritone.Tables;

namespace AfterHours.Models
{
    /// <summary>Authored, table-backed evidence used by the playable case.</summary>
    public sealed class CaseClueRow : ITableRow<string>
    {
        public string Id;
        public string Title;
        public string Detail;
        public string Key => Id;
    }

    /// <summary>Authored encounter tuning loaded by the offline profile.</summary>
    public sealed class EncounterSkillRow : ITableRow<string>
    {
        public string Id;
        public int FocusCost;
        public int Power;
        public int StunTurns;
        public string Key => Id;
    }
}

using System.Threading;
using System.Threading.Tasks;
using Tritone.Flows;
using Tritone.Kernel;

namespace AfterHours.Flows
{
    /// <summary>Base for named sample flows with no hidden external lifetime.</summary>
    public abstract class CaseFlow : IFlow
    {
        public Task EnterAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Update(in FrameTime time) { }
        public void Exit() { }
        public void Dispose() { }
    }

    public sealed class OfficeFlow : CaseFlow { }
    public sealed class InvestigationFlow : CaseFlow { }
    public sealed class EncounterFlow : CaseFlow { }
    public sealed class DebriefFlow : CaseFlow { }
}

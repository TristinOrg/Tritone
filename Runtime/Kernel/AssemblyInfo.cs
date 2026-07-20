using System.Runtime.CompilerServices;

// Allows the timing implementation assembly to access scheduler handles without exposing them publicly.
[assembly: InternalsVisibleTo("Tritone.Timing")]
[assembly: InternalsVisibleTo("Tritone.Tweening")]
[assembly: InternalsVisibleTo("Tritone.Unity")]

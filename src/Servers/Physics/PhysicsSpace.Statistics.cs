namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal readonly record struct Statistics(int Active, int Pairs, int Islands);
    internal Statistics PublishedStatistics;
    internal (int Active, int Pairs) ReportOnlyStatistics => (_reportOnlyActive, _reportOnlyPairs.Count);
    private void PublishStatistics() => PhysicsServer.Service.PublishStatistics(this, _backend.ReadStatistics());
}

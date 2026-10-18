namespace Electron2D;

public sealed partial class NavigationServer
{
    private NavigationAgentDebugSettings? _agentDebugSettings;
    internal NavigationAgentDebugSettings AgentDebugSettings
    {
        get
        {
            lock (_gate) return _agentDebugSettings ??= new(
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugAgentPaths),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugAgentPathColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugAgentPathPointSize));
        }
    }
}

internal sealed record NavigationAgentDebugSettings(bool Enabled, Color Color, float PointSize);

namespace Electron2D;

public sealed partial class NavigationServer
{
    private NavigationGeometryDebugSettings? _geometryDebugSettings;
    internal NavigationGeometryDebugSettings GeometryDebugSettings
    {
        get
        {
            lock (_gate) return _geometryDebugSettings ??= new(
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugEdgeConnectionColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugGeometryEdgeColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugGeometryFaceColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugGeometryEdgeDisabledColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugGeometryFaceDisabledColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugLinkConnectionColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugLinkConnectionDisabledColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugEdgeConnections),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugEdgeLines),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugGeometryFaceRandomColor),
                ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugLinkConnections),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugAgentsRadiusColor),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugObstaclesRadiusColor),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugStaticFacePushInColor),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugStaticEdgePushInColor),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugStaticFacePushOutColor),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugStaticEdgePushOutColor),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugAgentsRadius),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugObstaclesRadius),
                ProjectSettings.GetWithOverride(ProjectSettings.AvoidanceDebugObstaclesStatic));
        }
    }
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

internal sealed record NavigationGeometryDebugSettings(
    Color EdgeConnectionColor,
    Color GeometryEdgeColor,
    Color GeometryFaceColor,
    Color GeometryEdgeDisabledColor,
    Color GeometryFaceDisabledColor,
    Color LinkConnectionColor,
    Color LinkConnectionDisabledColor,
    bool EdgeConnections,
    bool EdgeLines,
    bool GeometryFaceRandomColor,
    bool LinkConnections,
    Color AgentsRadiusColor,
    Color ObstaclesRadiusColor,
    Color StaticFacePushInColor,
    Color StaticEdgePushInColor,
    Color StaticFacePushOutColor,
    Color StaticEdgePushOutColor,
    bool AgentsRadius,
    bool ObstaclesRadius,
    bool ObstaclesStatic);

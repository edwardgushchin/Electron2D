using Box2D.NET;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal readonly record struct Statistics(int Active, int Pairs, int Islands);
    internal Statistics PublishedStatistics;

    private void PublishStatistics()
    {
        var world = B2Worlds.b2GetWorldFromId(WorldID);
        var awake = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        var islands = 0;
        foreach (var sim in awake.islandSims.data.AsSpan(0, awake.islandSims.count))
        {
            var island = world.islands.data[sim.islandId];
            if (island.contactCount > 0 || island.jointCount > 0) islands++;
        }
        var pairs = checked(B2IdPools.b2GetIdCount(world.contactIdPool) + _reportOnlyPairs.Count);
        foreach (var sensor in world.sensors.data.AsSpan(0, world.sensors.count))
        {
            var shape = world.shapes.data[sensor.shapeId];
            var context = new SensorStatisticsQuery { World = world, Sensor = shape };
            foreach (var tree in world.broadPhase.trees)
                B2DynamicTrees.b2DynamicTree_Query(tree, shape.fatAABB, ulong.MaxValue, CountSensorCandidate, ref context);
            pairs = checked(pairs + context.Count);
        }
        PhysicsServer.Service.PublishStatistics(this, new(checked(awake.bodySims.count + _reportOnlyActive), pairs, islands));
    }

    private struct SensorStatisticsQuery { internal B2World World; internal B2Shape Sensor; internal int Count; }
    private static bool CountSensorCandidate(int proxy, ulong userData, ref SensorStatisticsQuery context)
    {
        var other = context.World.shapes.data[checked((int)userData)]; var sensor = context.Sensor;
        if (other.bodyId == sensor.bodyId || other.sensorIndex >= 0 && other.id < sensor.id) return true;
        if ((sensor.filter.maskBits & other.filter.categoryBits) != 0 ||
            other.sensorIndex >= 0 && (other.filter.maskBits & sensor.filter.categoryBits) != 0)
            context.Count = checked(context.Count + 1);
        return true;
    }
}

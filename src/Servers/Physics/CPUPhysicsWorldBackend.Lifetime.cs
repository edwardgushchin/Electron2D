using static Box2D.NET.B2DynamicTrees;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private void ReleaseSolverScratch()
    {
        try { if (_continuousTree is { } tree) b2DynamicTree_Destroy(tree); }
        finally
        {
            _continuousTree = null; _continuousBodies.Clear(); _continuousShapes.Clear(); _continuousProxies.Clear(); _continuousBoundaries.Clear();
            _continuousForces.Clear(); _continuousJointBudgets.Clear(); _continuousFinalize = null;
            _continuousBodies.Capacity = _continuousShapes.Capacity = _continuousProxies.Capacity = _continuousBoundaries.Capacity = 0;
            _continuousForces.Capacity = _continuousJointBudgets.Capacity = 0;
            _kinematicStepForces.Clear(); _jointImpulseVelocities.Clear(); _oneWayPairs.Clear(); _staleOneWayPairs.Clear();
            _frameContacts.Clear(); _frameContactIndices.Clear(); _frameContactHeads = []; _frameContactTails = []; _bodyMotions = [];
            _reportOnlyPairs.Clear(); _portableShapeLookup.Clear(); _colliders.Clear(); _jointAttachments.Clear();
        }
    }
}

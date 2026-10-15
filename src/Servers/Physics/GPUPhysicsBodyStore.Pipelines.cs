namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>Creates potentially needed numerical pipelines on the owner before a solver interval borrows the device.</summary>
    /// <remarks>Uses authored feature counts, without simulation, state readback or buffer-capacity changes. Warm calls reuse every handle.</remarks>
    internal void PrepareSimulationPipelines(in FieldParameters defaults)
    {
        EnsureAccess();
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Numerical pipeline preparation requires the world owner.");
        if (_highWater == 0) return;
        _spatialPipeline ??= _context.CreatePipeline("PhysicsResidentShapes.comp.spv");
        _residentTreePipeline ??= _context.CreatePipeline("PhysicsTree.comp.spv");
        _sleepPipeline ??= _context.CreatePipeline("PhysicsResidentSleep.comp.spv");
        _solverPipeline ??= _context.CreatePipeline("PhysicsResidentSolve.comp.spv");
        _solverUpdatePipeline ??= _context.CreatePipeline("PhysicsResidentUpdate.comp.spv");
        _solverGatherPipeline ??= _context.CreatePipeline("PhysicsResidentGather.comp.spv");
        if (_activeAreaCount > 0 || !UniformFields(defaults)) _fieldPipeline ??= _context.CreatePipeline("PhysicsResidentFields.comp.spv");
        if (_jointHighWater > 0 || _exceptionHighWater > 0) _jointEditPipeline ??= _context.CreatePipeline("PhysicsResidentJointEdits.comp.spv");
        if (JointCount > 0) _jointPreparePipeline ??= _context.CreatePipeline("PhysicsResidentJoints.comp.spv");
        if (_captureReports) _reportPipeline ??= _context.CreatePipeline("PhysicsResidentReports.comp.spv");
        if (ShapeCount < 2) return;
        // Contacts are device results, so prepare their possible kernels before discovering pairs on the numerical lane.
        _contactPipeline ??= _context.CreatePipeline("PhysicsResidentContacts.comp.spv");
        _colorPipeline ??= _context.CreatePipeline("PhysicsResidentColors.comp.spv");
        _coloredSolvePipeline ??= _context.CreatePipeline("PhysicsResidentColoredSolve.comp.spv");
        if (PackColoredContacts) _packSolverPipeline ??= _context.CreatePipeline("PhysicsResidentPackSolver.comp.spv");
        if (_ccdBodyCount > 0 || _kinematicBodyCount > 0) _ccdPipeline ??= _context.CreatePipeline("PhysicsResidentCCD.comp.spv");
    }
}

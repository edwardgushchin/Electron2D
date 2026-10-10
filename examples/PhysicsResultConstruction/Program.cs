using Electron2D;
using Electron2D.Examples.PhysicsResultConstruction;

ResultConstructionChecks.Run(args is ["gpu"] ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);

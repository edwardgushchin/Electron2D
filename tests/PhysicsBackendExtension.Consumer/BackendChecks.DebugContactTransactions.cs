using Electron2D;

public static partial class BackendChecks
{
    private static void DebugContactTransactions()
    {
        ProjectSettings.Set(ProjectSettings.DebugCollisionMaxContacts, 3);
        foreach (var rollbackFailures in new[] { 0, 1, 2 })
            foreach (var nested in new[] { false, true })
            {
                var solvers = new Solver[3]; var worlds = new World[3];
                using var root = new SubViewport();
                for (var i = 0; i < worlds.Length; i++)
                {
                    var index = i; var name = $"DebugTransaction-{rollbackFailures}-{nested}-{i}";
                    PhysicsServerManager.RegisterServer(name, () => solvers[index] = new());
                    worlds[i] = new World(name);
                    var view = new SubViewport { World = worlds[i] }; view.AddChild(new StaticBody()); root.AddChild(view);
                }
                using var tree = new SceneTree(root);
                Exception failure = nested ? new AggregateException("Provider diagnostic failure.", new ApplicationException("First provider cause."), new ApplicationException("Second provider cause.")) :
                    new ApplicationException("Original diagnostic capacity failure.");
                var rollback = new[] { new ApplicationException("First diagnostic rollback failure."), new ApplicationException("Second diagnostic rollback failure.") };
                var attempts = new int[3]; Exception? caught = null;
                try
                {
                    for (var i = 0; i < solvers.Length; i++)
                    {
                        var index = i;
                        solvers[i].DebugPolicyHook = maximum =>
                        {
                            if (maximum > 0 && index == 2) throw failure;
                            if (maximum == 0)
                            {
                                attempts[index]++;
                                if (index < rollbackFailures) throw rollback[index];
                            }
                        };
                    }
                    try { tree.DebugCollisionsHint = true; } catch (Exception error) { caught = error; }
                    if (rollbackFailures == 0) Check(ReferenceEquals(caught, failure), "Single diagnostic preparation error retains its exact instance");
                    else if (caught is not AggregateException aggregate || aggregate.InnerExceptions.Count != rollbackFailures + 1 ||
                        !ReferenceEquals(aggregate.InnerExceptions[0], failure) ||
                        !aggregate.InnerExceptions.Skip(1).SequenceEqual(rollback.Take(rollbackFailures)))
                        throw new InvalidOperationException("Diagnostic rollback masked the original preparation error or skipped later failures.", caught);
                    Check(!tree.DebugCollisionsHint && attempts[0] == 1 && attempts[1] == 1,
                        "Failed enable keeps the tree disabled and attempts every previously prepared world's rollback");
                    Check(solvers[2].DebugLimit == 0 && solvers[0].Steps == 0 && solvers[1].Steps == 0 && solvers[2].Steps == 0,
                        "Failed preparation does not solve or enable the rejecting world");
                    foreach (var solver in solvers) solver.DebugPolicyHook = null;
                    tree.PhysicsFrame(.01);
                    Check(solvers.All(s => s.DebugLimit == 0 && s.DebugReads == 0 && s.Steps == 1),
                        "The next ordinary frame clears remaining requests before solving, without diagnostic reads");
                    tree.DebugCollisionsHint = true; tree.PhysicsFrame(.01);
                    Check(solvers.All(s => s.DebugLimit == 3 && s.DebugReads == 1 && s.Steps == 2), "A later explicit enable prepares every world and publishes empty samples");
                    tree.DebugCollisionsHint = false;
                }
                finally
                {
                    foreach (var solver in solvers) solver.DebugPolicyHook = null;
                    tree.Dispose();
                    foreach (var world in worlds) world.Dispose();
                }
            }
        Console.WriteLine("Diagnostic preparation: original errors, all-world rollback attempts and retry without replay passed.");
    }
}

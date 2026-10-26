namespace Electron2D.Editor;

public sealed partial class ProjectSession
{
    private SessionJob? _activeJob;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SessionJob> _jobs = new();
    private long _jobSequence;
    internal IReadOnlyList<SessionJob> Jobs() { Check(); FlushCodeBuffer(); return Array.AsReadOnly(_jobs.Values.OrderBy(j => j.Sequence).ToArray()); }
    internal void CancelJobs(IEnumerable<Guid> ids) { foreach (var id in ids) if (_jobs.TryGetValue(id, out var job)) job.Cancel(); }
    internal void ForgetJob(Guid id) { if (_jobs.TryRemove(id, out var job)) Interlocked.CompareExchange(ref _activeJob, null, job); }
    internal bool IsSnapshotStale(ulong scene, ulong code) { Check(); FlushCodeBuffer(); return _state.SceneRevision != scene || _state.CodeRevision != code; }

    internal SessionJob StartJob(SessionJobOptions options, ulong expectedSceneRevision, ulong expectedCodeRevision)
    {
        Begin();
        try
        {
            ArgumentNullException.ThrowIfNull(options);
            if (_activeJob is { Completion.IsCompleted: false }) throw new InvalidOperationException("This session already owns an active job; cancel or finish it first.");
            if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("The current snapshot process-group owner requires Linux.");
            Expected(SessionDocumentKind.Scene, expectedSceneRevision); Expected(SessionDocumentKind.Code, expectedCodeRevision);
            if (External(SessionDocumentKind.Scene) || External(SessionDocumentKind.Code)) throw Conflict(Guid.Empty, expectedSceneRevision, _state.SceneRevision);
            var project = _storage.Resolve(options.ProjectFile);
            if (IOPath.GetDirectoryName(project) != _storage.Root || IOPath.GetExtension(project) != ".csproj" || !File.Exists(project))
                throw new ArgumentException("The first snapshot profile requires one root-level .csproj.", nameof(options));
            options = options with { ProjectFile = IOPath.GetFileName(project), SDKDirectory = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(options.SDKDirectory)), CaptureSteps = options.CaptureSteps?.ToArray() ?? [], Inputs = options.Inputs?.ToArray() ?? [] };
            if (!Enum.IsDefined(options.Kind) || options.DeadlineMilliseconds is < 100 or > 3600000) throw new ArgumentOutOfRangeException(nameof(options));
            var validate = new RunRequest
            {
                SnapshotID = Guid.NewGuid(),
                SnapshotSHA256 = new string('0', 64),
                Scene = _state.ScenePath,
                Mode = options.Mode,
                Steps = options.Steps,
                FPS = options.FPS,
                Width = options.Width,
                Height = options.Height,
                Backend = options.Backend,
                DeadlineMilliseconds = options.DeadlineMilliseconds,
                CaptureSteps = options.CaptureSteps.ToArray(),
                Inputs = options.Inputs.ToArray()
            };
            validate.Validate();
            var source = SnapshotStore.Inventory(_storage.Root, false); var sdk = SnapshotStore.Inventory(options.SDKDirectory, true);
            if (source.Any(f => f.Path is "Directory.Build.props" or "Directory.Build.targets" or ".session-nodes.json"))
                throw new NotSupportedException("Directory build policies and reserved snapshot metadata need an explicit dependency profile.");
            CheckJobBudget(source.Sum(f => f.Size) + sdk.Sum(f => f.Size), (long)options.Width * options.Height * 4 * options.CaptureSteps.Count);
            var data = new CapturedProject(_storage.Root, _state.ScenePath, _state.CodePath, _state.SceneID, _state.CodeID,
                _state.SceneRevision, _state.CodeRevision, _state.Scene, _state.Nodes, SessionStorage.CodeBytes(_state.Code, _state.BOM), source, sdk, options);
            var output = _storage.Resolve(".electron2d/jobs/" + Guid.NewGuid().ToString("N"), metadata: true);
            if (OperatingSystem.IsWindows()) Directory.CreateDirectory(output);
            else Directory.CreateDirectory(output, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var job = new SessionJob(this, data, output, checked(++_jobSequence));
            _jobs[job.ID] = job; return _activeJob = job;
        }
        finally { _busy = false; }
    }

    private void CheckJobBudget(long inputs, long captures)
    {
        const long budget = 1024L * 1024 * 1024, reserve = 512L * 1024 * 1024;
        var root = _storage.Resolve(".electron2d/jobs", metadata: true);
        long retained = 0;
        if (Directory.Exists(root))
        {
            foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                JobProtocol.ChildPath(root, IOPath.GetRelativePath(root, path));
                retained = checked(retained + new FileInfo(path).Length);
            }
        }
        var estimated = checked(inputs * 2 + captures + 64L * 1024 * 1024);
        if (retained + estimated > budget) throw new IOException("Generated jobs exceed the one-GiB project budget; clear completed results before starting another job.");
        if (new DriveInfo(_storage.Root).AvailableFreeSpace < estimated + reserve) throw new IOException("Insufficient free disk space for this snapshot/build and a 512-MiB reserve.");
    }

    internal Node CreatePreview(ulong expectedSceneRevision)
    {
        Begin();
        try
        {
            Expected(SessionDocumentKind.Scene, expectedSceneRevision);
            using var source = Decode(_storage, _state.Scene, PathFor(SessionDocumentKind.Scene), _state.Nodes);
            foreach (var node in Flatten(source))
                if (node.GetType() != typeof(Node) && node.GetType() != typeof(Entity) && node.GetType() != typeof(Polygon) && node.GetType() != typeof(Line) && node.GetType() != typeof(Sprite))
                    throw new NotSupportedException($"Authoring preview has no passive projection for {node.GetType().Name}; run the frozen snapshot for actual gameplay output.");
            // Static built-in drawing only: no project classes, event connections or active gameplay node roles.
            var copy = source.Duplicate(0);
            try { SceneOverlay.Apply(copy, _state.Nodes); return copy; }
            catch { copy.Dispose(); throw; }
        }
        finally { _busy = false; }
    }
}

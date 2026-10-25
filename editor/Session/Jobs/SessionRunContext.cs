namespace Electron2D.Editor;

/// <summary>Provides bounded observations, assertions and explicit completion for one scenario.</summary>
/// <remarks>The root and random generator are borrowed. Calls run on the engine owner thread. Reporting is
/// allocating tooling work, outside engine allocation measurements. A seed describes only this generator.</remarks>
public sealed class SessionRunContext
{
    private readonly RunRequest _request;
    private readonly RunReport _report;
    private Node? _root;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private bool _released;
    private int _reportBytes;
    internal bool Completed;
    internal SessionRunContext(RunRequest request, RunReport report) { _request = request; _report = report; Random = new() { Seed = request.Seed }; }
    /// <summary>Gets the execution mode.</summary>
    public SessionRunMode Mode => _request.Mode;
    /// <summary>Gets the current one-based step, or zero before simulation.</summary>
    public int Step { get; internal set; }
    /// <summary>Gets the maximum number of completed steps.</summary>
    public int StepBudget => _request.Steps;
    /// <summary>Gets the fixed process and physics cadence in frames per second.</summary>
    public int FPS => _request.FPS;
    /// <summary>Gets the requested root canvas width in pixels.</summary>
    public int Width => _request.Width;
    /// <summary>Gets the requested root canvas height in pixels.</summary>
    public int Height => _request.Height;
    /// <summary>Gets the borrowed scene root after factory construction or file loading.</summary>
    /// <exception cref="InvalidOperationException">Configuration or creation has not published a root yet.</exception>
    public Node Root { get { Access(); return _root ?? throw new InvalidOperationException("The root is available after scene creation."); } }
    /// <summary>Gets the run's borrowed seeded generator.</summary>
    public RandomNumberGenerator Random { get; }
    internal void Attach(Node root) => _root = root;
    private void Access()
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("The run context requires its owner thread.");
    }
    internal void Release() { Access(); Random.Dispose(); _released = true; }
    private void CheckName(string name)
    {
        Access();
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length is < 1 or > 256 || _report.Observations.Count + _report.Assertions.Count >= 4096)
            throw new InvalidOperationException("Reports permit 4096 entries with names of 1..256 characters.");
        AddBudget(512 + 6 * name.Length);
    }
    private void AddBudget(int bytes)
    {
        if (bytes > 4 * 1024 * 1024 - _reportBytes) throw new InvalidOperationException("SessionObservation and assertion data exceeds the four-MiB JSON budget.");
        _reportBytes += bytes;
    }
    /// <summary>Records a finite numeric observation at the current step.</summary>
    /// <param name="name">A stable observation name.</param><param name="value">A finite numeric value.</param>
    public void Observe(string name, double value) { CheckName(name); if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _report.Observations.Add(new(Step, name, Number: value)); }
    /// <summary>Records a boolean observation at the current step.</summary>
    /// <param name="name">A stable observation name.</param><param name="value">The observed flag.</param>
    public void Observe(string name, bool value) { CheckName(name); _report.Observations.Add(new(Step, name, Boolean: value)); }
    /// <summary>Records up to 4096 characters of text at the current step.</summary>
    /// <param name="name">A stable observation name.</param><param name="value">Non-null text.</param>
    public void Observe(string name, string value) { CheckName(name); ArgumentNullException.ThrowIfNull(value); if (value.Length > 4096) throw new ArgumentOutOfRangeException(nameof(value)); AddBudget(6 * value.Length); _report.Observations.Add(new(Step, name, Text: value)); }
    /// <summary>Records an assertion and terminates the run if it fails.</summary>
    /// <param name="name">A stable assertion name.</param><param name="condition">The required outcome.</param><param name="message">A concise explanation, limited to 4096 characters.</param>
    public void Check(string name, bool condition, string message)
    {
        CheckName(name); ArgumentNullException.ThrowIfNull(message); if (message.Length > 4096) throw new ArgumentOutOfRangeException(nameof(message));
        AddBudget(6 * message.Length); _report.Assertions.Add(new(Step, name, condition, message));
        if (!condition) { if (_report.ExitCode == 0) _report.ExitCode = 5; throw new JobFailure("ASSERTION_FAILED", 5, message); }
    }
    /// <summary>Requests success after the current step completes; reaching the budget alone is a failure.</summary>
    /// <exception cref="InvalidOperationException">Simulation has not started.</exception>
    public void Complete() { Access(); if (Step == 0) throw new InvalidOperationException("Complete a running simulation step."); Completed = true; }
}

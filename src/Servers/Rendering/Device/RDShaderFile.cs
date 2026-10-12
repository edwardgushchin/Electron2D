using System.Buffers;
using System.Text;

namespace Electron2D;

/// <summary>Groups named compiled shader resources and an import-level diagnostic.</summary>
/// <remarks>Version names are case-sensitive Unicode strings; the empty name is the default version.
/// Modules are borrowed and preserve identity. This resource stores import results without compiling or
/// creating device objects. Games pass a selected module to RenderingDevice.ShaderCreateFromSPIRV.
/// Individual operations serialize storage; callers synchronize whole authoring and file-save sequences.</remarks>
public sealed class RDShaderFile : Resource
{
    private readonly object _gate = new();
    private Dictionary<string, RDShaderSPIRV> _versions = new(StringComparer.Ordinal);
    private string _baseError = "";
    private string[]? _pendingNames;
    /// <summary>Creates an empty version collection with no import diagnostic.</summary>
    public RDShaderFile() { }
    /// <summary>Gets or sets the import-level diagnostic independently of stage results.</summary>
    /// <value>Empty initially. A diagnostic does not hide versions already supplied by the importer.</value>
    /// <exception cref="ArgumentNullException">The value is null.</exception>
    public string BaseError
    {
        get { lock (_gate) { ThrowIfDisposed(); return _baseError; } }
        set { ArgumentNullException.ThrowIfNull(value); lock (_gate) { ThrowIfDisposed(); _baseError = value; } EmitChanged(); }
    }
    /// <summary>Associates a live compiled resource with an exact version name.</summary>
    /// <param name="bytecode">Borrowed live module; its stage bytecode and diagnostics are preserved.</param>
    /// <param name="version">Case-sensitive version name; empty selects the default.</param>
    /// <remarks>Replacing a version retains the supplied module identity and does not dispose its predecessor.
    /// Every successful assignment publishes Changed after commitment, including an equal assignment.
    /// Replacements of prepared names allocate no engine-managed storage.</remarks>
    /// <exception cref="ArgumentNullException">The module or name is null.</exception>
    /// <exception cref="ArgumentException">The name contains a zero character or invalid Unicode.</exception>
    /// <exception cref="ObjectDisposedException">This resource or the module is disposed.</exception>
    public void SetBytecode(RDShaderSPIRV bytecode, string version = "")
    {
        ArgumentNullException.ThrowIfNull(bytecode); ValidateName(version);
        lock (_gate) { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(bytecode.IsDisposed, bytecode); _versions[version] = bytecode; _pendingNames = null; }
        EmitChanged();
    }
    /// <summary>Returns the borrowed module associated with an exact version name.</summary>
    /// <param name="version">Case-sensitive version name; empty selects the default.</param>
    /// <returns>The same supplied module, or null when that name is absent.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name contains a zero character or invalid Unicode.</exception>
    /// <exception cref="ObjectDisposedException">This resource or the selected borrowed module is disposed.</exception>
    public RDShaderSPIRV? GetSPIRV(string version = "")
    {
        ValidateName(version);
        lock (_gate) { ThrowIfDisposed(); if (!_versions.TryGetValue(version, out var value)) return null; ObjectDisposedException.ThrowIf(value.IsDisposed, value); return value; }
    }
    /// <summary>Returns an independent sorted list of current version names.</summary>
    /// <returns>Names in Unicode scalar order, including an empty default name when present.</returns>
    /// <remarks>This is a cold copying query. Disposing a borrowed module does not remove its name.</remarks>
    public string[] GetVersionList()
    {
        lock (_gate) { ThrowIfDisposed(); var names = _versions.Keys.ToArray(); Array.Sort(names, CompareNames); return names; }
    }
    private static void ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        for (var span = name.AsSpan(); !span.IsEmpty;)
        {
            if (Rune.DecodeFromUtf16(span, out var rune, out var consumed) != OperationStatus.Done || rune.Value == 0) throw new ArgumentException("Version names require nonzero Unicode scalar values.", nameof(name));
            span = span[consumed..];
        }
    }
    private static int CompareNames(string? a, string? b)
    {
        var left = a.AsSpan(); var right = b.AsSpan();
        while (!left.IsEmpty && !right.IsEmpty)
        {
            Rune.DecodeFromUtf16(left, out var x, out var nx); Rune.DecodeFromUtf16(right, out var y, out var ny);
            var order = x.Value.CompareTo(y.Value); if (order != 0) return order; left = left[nx..]; right = right[ny..];
        }
        return left.Length.CompareTo(right.Length);
    }
    private void RestoreNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names); var copy = (string[])names.Clone(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in copy) { ValidateName(name); if (!seen.Add(name)) throw new InvalidDataException("Duplicate shader versions."); }
        lock (_gate) { ThrowIfDisposed(); _pendingNames = copy; }
    }
    private RDShaderSPIRV[] StoredModules()
    {
        lock (_gate) { var names = GetVersionList(); return names.Select(name => _versions[name]).ToArray(); }
    }
    private void RestoreModules(RDShaderSPIRV[] modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        lock (_gate)
        {
            ThrowIfDisposed(); var names = _pendingNames ?? throw new InvalidDataException("Shader version names must precede modules.");
            if (modules.Length != names.Length) throw new InvalidDataException("Shader version/module counts differ.");
            var versions = new Dictionary<string, RDShaderSPIRV>(names.Length, StringComparer.Ordinal);
            for (var i = 0; i < names.Length; i++) { var module = modules[i]; if (module is null || module.IsDisposed) throw new InvalidDataException("Shader versions require live modules."); versions.Add(names[i], module); }
            _versions = versions; _pendingNames = null;
        }
        EmitChanged();
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDShaderFile, string>(nameof(BaseError), p => p.BaseError, (p, v) => p.BaseError = v, _ => "", stored: true),
        new PropertyDescriptor<RDShaderFile, string[]>("_version_names", p => p.GetVersionList(), (p, v) => p.RestoreNames(v), _ => [], stored: true),
        new PropertyDescriptor<RDShaderFile, RDShaderSPIRV[]>("_version_modules", p => p.StoredModules(), (p, v) => p.RestoreModules(v), _ => [], stored: true)
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new RDShaderFile();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        KeyValuePair<string, RDShaderSPIRV>[] entries; string error;
        lock (_gate) { ThrowIfDisposed(); entries = _versions.ToArray(); error = _baseError; }
        var versions = new Dictionary<string, RDShaderSPIRV>(entries.Length, StringComparer.Ordinal);
        foreach (var entry in entries) versions.Add(entry.Key, (RDShaderSPIRV)duplicateSubresource(entry.Value)!);
        var copy = (RDShaderFile)target; lock (copy._gate) { copy.ThrowIfDisposed(); copy._versions = versions; copy._baseError = error; copy._pendingNames = null; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { _versions.Clear(); _pendingNames = null; _baseError = ""; } base.Dispose(disposing); }
}

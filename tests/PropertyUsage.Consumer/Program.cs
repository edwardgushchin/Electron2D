using Electron2D;
using Path = System.IO.Path;

ResourceFileTypes.RegisterNode("tests.PropertyUsageProbe", Probe.Create);
using var owner = new Probe();
var list = owner.GetPropertyList();
Check(list.Contains(Probe.Stored) && list.Contains(Probe.Transient) && list.Contains(Probe.ReadOnly), "Discovery retains every access/storage role.");
Check(Probe.Stored.IsStored && !Probe.Stored.IsReadOnly && !Probe.Transient.IsStored && !Probe.Transient.IsReadOnly && Probe.ReadOnly.IsReadOnly && !Probe.ReadOnly.IsStored, "Independent typed roles.");
owner.SetIndexed(Probe.Stored, 41); owner.SetIndexed(Probe.Transient, 73);
Check(owner.GetIndexed(Probe.ReadOnly) == 41, "Readonly discovery can read live state.");
Reject<InvalidOperationException>(() => owner.SetIndexed(Probe.ReadOnly, 2));
Reject<ArgumentException>(() => new PropertyDescriptor<Probe, int>("Bad", p => p.Value, stored: true));
Reject<ArgumentException>(() => new PropertyDescriptor<Probe, int>("Bad", p => p.Value, revertValue: _ => 1));
Reject<ArgumentOutOfRangeException>(() => Probe.Stored.SetValue(owner, -1));
using var scene = new PackedScene();
scene.Pack(owner);
var state = scene.GetState();
var names = Enumerable.Range(0, state.GetNodePropertyCount(0)).Select(i => state.GetNodePropertyName(0, i)).ToArray();
Check(names.Contains("Stored") && !names.Contains("Transient") && !names.Contains("ReadOnly"), "Only stored values captured.");
using (var instance = (Probe)scene.Instantiate()) Check(instance.Value == 41 && instance.Temporary == 5, "Memory restoration excludes transient state.");
var path = Path.Combine(Path.GetTempPath(), $"property-usage-{Guid.NewGuid():N}.e2dscene");
try
{
    foreach (var flags in new[] { SaverFlags.None, SaverFlags.Compress, SaverFlags.SaveBigEndian, SaverFlags.Compress | SaverFlags.SaveBigEndian })
    {
        ResourceSaver.Save(scene, path, flags);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var instance = (Probe)loaded.Instantiate();
        Check(instance.Value == 41 && instance.Temporary == 5, "File restoration uses typed setters only for stored values.");
    }
}
finally { if (File.Exists(path)) File.Delete(path); }
owner.BeforeRead = () => Check(Probe.Transient.GetValue(owner) == 73, "Getter can reenter another typed descriptor.");
Check(Probe.Stored.GetValue(owner) == 41, "Reentrant read completes."); owner.BeforeRead = null;
owner.FailRead = true; Reject<ArithmeticException>(() => scene.Pack(owner)); owner.FailRead = false;
scene.Pack(owner);
owner.Dispose(); Reject<ObjectDisposedException>(() => Probe.Stored.GetValue(owner)); Reject<ObjectDisposedException>(() => Probe.Transient.SetValue(owner, 1));
Console.WriteLine("Property usage: discovery/access/storage, file and memory restoration, transient exclusion, invalid metadata, validation, getter failure/retry, reentry and lifetime passed.");

static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
sealed class Probe : Node
{
    internal int Value = 7, Temporary = 5;
    internal Action? BeforeRead;
    internal bool FailRead;
    internal static readonly PropertyDescriptor<Probe, int> Stored = new("Stored", p => { p.BeforeRead?.Invoke(); return p.FailRead ? throw new ArithmeticException() : p.Value; }, (p, v) => p.Value = v, validator: (_, v) => v >= 0, stored: true);
    internal static readonly PropertyDescriptor<Probe, int> Transient = new("Transient", p => p.Temporary, (p, v) => p.Temporary = v);
    internal static readonly PropertyDescriptor<Probe, int> ReadOnly = new("ReadOnly", p => p.Value);
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([Stored, Transient, ReadOnly]);
    internal static Probe Create() => new();
    protected override Func<Node> CreateSceneInstanceFactory() => Create;
}

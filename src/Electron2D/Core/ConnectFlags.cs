namespace Electron2D;

[Flags]
public enum ConnectFlags
{
    None = 0,
    Deferred = 1,
    Persist = 2,
    OneShot = 4,
    ReferenceCounted = 8,
    AppendSourceObject = 16
}

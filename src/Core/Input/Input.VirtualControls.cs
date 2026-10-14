namespace Electron2D;

public sealed partial class Input
{
    internal static bool SetVirtualContribution(ulong owner, int slot, string action, float strength)
    {
        if (action.Length == 0 || !InputMap.HasAction(action)) return false;
        lock (SharedInstance._gate)
        {
            SharedInstance.UpdateContribution(action, new ActionSource(InputEvent.DeviceIdInternal, MaxEventsPerAction + 1 + slot, owner),
                strength > 0 ? new InputActionMatch(true, strength, strength) : default, exact: true, eventId: 0);
        }
        return true;
    }
}

internal sealed class VirtualActionBinding(ElectronObject owner, int slot)
{
    private readonly ulong _owner = owner.InstanceID;
    private string _action = "";
    internal string Action => _action;
    internal void Set(string action, float strength)
    {
        if (_action != action) Release();
        if (Input.SetVirtualContribution(_owner, slot, action, strength)) _action = strength > 0 ? action : "";
    }
    internal void Release()
    { if (_action.Length != 0) Input.SetVirtualContribution(_owner, slot, _action, 0); _action = ""; }
}

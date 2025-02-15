namespace Electron2D.Input;

public struct PowerInfo
{
    internal PowerInfo(PowerState state, int percent)
    {
        State = state;
        Percent = percent;
    }

    public PowerState State { get; }
    
    public int Percent { get; }
}
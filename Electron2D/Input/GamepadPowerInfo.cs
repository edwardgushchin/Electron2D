namespace Electron2D.Input;

public struct PowerInfo
{
    internal PowerInfo(PowerState state, int seconds, int percent)
    {
        State = state;
        Seconds = seconds;
        Percent = percent;
    }

    public PowerState State { get; }
    
    public int Seconds { get; }
    
    public int Percent { get; }
}
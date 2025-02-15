using SDL3;

namespace Electron2D.Input;


/// <summary>
/// <para>The basic state for the system's power supply.</para>
/// </summary>
public enum PowerState
{
    /// <summary>
    /// cannot determine power status
    /// </summary>
    Unknown = SDL.PowerState.Unknown,
        
    /// <summary>
    /// Not plugged in, running on the battery
    /// </summary>
    OnBattery = SDL.PowerState.OnBattery,
        
    /// <summary>
    /// Plugged in, no battery available
    /// </summary>
    NoBattery = SDL.PowerState.NoBattery,
        
    /// <summary>
    /// Plugged in, charging battery
    /// </summary>
    Charging = SDL.PowerState.Charging,
        
    /// <summary>
    /// Plugged in, battery charged
    /// </summary>
    Charged = SDL.PowerState.Charged
}
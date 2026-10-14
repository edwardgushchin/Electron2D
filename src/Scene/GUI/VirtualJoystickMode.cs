namespace Electron2D;

/// <summary>Selects where a virtual joystick's base is placed during a contact.</summary>
public enum VirtualJoystickMode
{
    /// <summary>The base stays at its authored offset; contact starts inside its base rectangle.</summary>
    Fixed = 0,
    /// <summary>The base starts at the accepted contact position.</summary>
    Dynamic = 1,
    /// <summary>The base follows a contact moving beyond its clamp radius within the control.</summary>
    Following = 2,
}

/// <summary>Selects whether virtual joystick decoration is always drawn.</summary>
public enum JoystickVisibility
{
    /// <summary>Draw while the control is visible.</summary>
    Always = 0,
    /// <summary>Draw only while a contact is active.</summary>
    WhenTouched = 1,
}

/// <summary>Selects the touch-button device visibility policy.</summary>
public enum TouchButtonVisibility
{
    /// <summary>Draw and accept touch input on every host.</summary>
    Always = 0,
    /// <summary>Draw and accept touch input only when a touchscreen is available.</summary>
    TouchscreenOnly = 1,
}

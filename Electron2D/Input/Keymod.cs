using SDL3;

namespace Electron2D;

[Flags]
public enum Keymod : ushort
{ 
    /// <summary>
    /// no modifier is applicable.
    /// </summary>
    None = SDL.Keymod.None,
    
    /// <summary>
    /// the left Shift key is down.
    /// </summary>
    LShift = SDL.Keymod.LShift,
    
    /// <summary>
    /// the right Shift key is down.
    /// </summary>
    RShift = SDL.Keymod.RShift,
    
    /// <summary>
    /// the Level 5 Shift key is down.
    /// </summary>
    Level5 = SDL.Keymod.Level5, 
    
    /// <summary>
    /// the left Ctrl (Control) key is down.
    /// </summary>
    LCtrl = SDL.Keymod.LCtrl,
    
    /// <summary>
    /// the right Ctrl (Control) key is down.
    /// </summary>
    RCtrl = SDL.Keymod.RCtrl,
    
    /// <summary>
    /// the left Alt key is down.
    /// </summary>
    LAlt = SDL.Keymod.LAlt,
    
    /// <summary>
    /// the right Alt key is down.
    /// </summary>
    RAlt = SDL.Keymod.RAlt,
    
    /// <summary>
    /// the left GUI key (often the Windows key) is down.
    /// </summary>
    LGUI = SDL.Keymod.LGUI,
    
    /// <summary>
    /// the right GUI key (often the Windows key) is down.
    /// </summary>
    RGUI = SDL.Keymod.RGUI,
    
    /// <summary>
    /// the Num Lock key (may be located on an extended keypad) is down.
    /// </summary>
    Num = SDL.Keymod.Num,
    
    /// <summary>
    /// the Caps Lock key is down.
    /// </summary>
    Caps = SDL.Keymod.Caps,
    
    /// <summary>
    /// the !AltGr key is down.
    /// </summary>
    Mode = SDL.Keymod.Mode,
    
    /// <summary>
    /// the Scroll Lock key is down.
    /// </summary>
    Scroll = SDL.Keymod.Scroll,
    
    /// <summary>
    /// Any Ctrl key is down.
    /// </summary>
    Ctrl = LCtrl | RCtrl,
    
    /// <summary>
    /// Any Shift key is down.
    /// </summary>
    Shift = LShift | RShift,
    
    /// <summary>
    /// Any Alt key is down.
    /// </summary>
    Alt = LAlt | RAlt,
    
    /// <summary>
    /// Any GUI key is down.
    /// </summary>p0
    GUI = LGUI | RGUI
    }
using SDL3;

namespace Electron2D.Input;

public enum GamepadButton
{
    Invalid = SDL.GamepadButton.Invalid,
    
    /// <summary>
    /// Bottom face button (e.g. Xbox A button)
    /// </summary>
    South = SDL.GamepadButton.South,
    
    /// <summary>
    /// Right face button (e.g. Xbox B button)
    /// </summary>
    East = SDL.GamepadButton.East,
    
    /// <summary>
    /// Left face button (e.g. Xbox X button)
    /// </summary>
    West = SDL.GamepadButton.West,
    
    /// <summary>
    /// Top face button (e.g. Xbox Y button)
    /// </summary>
    North = SDL.GamepadButton.North,
    Back = SDL.GamepadButton.Back,
    Guide = SDL.GamepadButton.Guide,
    Start = SDL.GamepadButton.Start,
    LeftStick = SDL.GamepadButton.LeftStick,
    RightStick = SDL.GamepadButton.RightStick,
    LeftShoulder = SDL.GamepadButton.LeftShoulder,
    RightShoulder = SDL.GamepadButton.RightShoulder,
    DPadUp = SDL.GamepadButton.DPadUp,
    DPadDown = SDL.GamepadButton.DPadDown,
    DPadLeft = SDL.GamepadButton.DPadLeft,
    DPadRight = SDL.GamepadButton.DPadRight,
    
    /// <summary>
    /// Additional button (e.g. Xbox Series X share button, PS5 microphone button, Nintendo Switch Pro capture button, Amazon Luna microphone button, Google Stadia capture button)
    /// </summary>
    Misc1 = SDL.GamepadButton.Misc1,
    
    /// <summary>
    /// Upper or primary paddle, under your right hand (e.g. Xbox Elite paddle P1)
    /// </summary>
    RightPaddle1 = SDL.GamepadButton.RightPaddle1,
    
    /// <summary>
    /// Upper or primary paddle, under your left hand (e.g. Xbox Elite paddle P3)
    /// </summary>
    LeftPaddle1 = SDL.GamepadButton.LeftPaddle1,
    
    /// <summary>
    /// Lower or secondary paddle, under your right hand (e.g. Xbox Elite paddle P2)
    /// </summary>
    RightPaddle2 = SDL.GamepadButton.RightPaddle2,
    
    /// <summary>
    /// Lower or secondary paddle, under your left hand (e.g. Xbox Elite paddle P4)
    /// </summary>
    LeftPaddle2 = SDL.GamepadButton.LeftPaddle2,
    
    /// <summary>
    /// PS4/PS5 touchpad button
    /// </summary>
    Touchpad = SDL.GamepadButton.Touchpad,
    
    /// <summary>
    /// Additional button
    /// </summary>
    Misc2 = SDL.GamepadButton.Misc2,
    
    /// <summary>
    /// Additional button
    /// </summary>
    Misc3 = SDL.GamepadButton.Misc3,
    
    /// <summary>
    /// Additional button
    /// </summary>
    Misc4 = SDL.GamepadButton.Misc4,
    
    /// <summary>
    /// Additional button
    /// </summary>
    Misc5 = SDL.GamepadButton.Misc5,
    
    /// <summary>
    /// Additional button
    /// </summary>
    Misc6 = SDL.GamepadButton.Misc6,
    Count = SDL.GamepadButton.Count,
}
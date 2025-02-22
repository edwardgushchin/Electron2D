using Electron2D.Input;

namespace Electron2D;

public abstract class Scene
{ 
    private Color _clearColor = Color.Black;
    
    public IRenderContext? RenderContext { get; set; }
    
    public Color ClearColor
    {
        get => _clearColor;
        set
        {
            _clearColor = value;
            RenderContext?.SetClearColor(_clearColor);
        }
    }
    
    public abstract void OnStart();
    
    public abstract void OnLoad();
    
    public abstract void Update(float deltaTime);
    
    public abstract void Render();
    
    public abstract void Shutdown();

    #region Window events
    
    /// <summary>
    /// Window has been shown
    /// </summary>
    public virtual void OnWindowShown() { }

    /// <summary>
    /// Window has been hidden
    /// </summary>
    public virtual void OnWindowHidden() { }
    
    /// <summary>
    /// Window has been moved
    /// </summary>
    /// <param name="x">New window position by X coordinate</param>
    /// <param name="y">New window position by Y coordinate</param>
    public virtual void OnWindowMoved(int x, int y) { }

    public virtual void OnWindowResized(int width, int height) { }
    
    public virtual void OnWindowMinimized() { }
    
    public virtual void OnWindowMaximized() { }
    
    public virtual void OnWindowRestored() { }
    
    public virtual void OnWindowFocusGained() { }
    
    public virtual void OnWindowFocusLost() { }
    
    public virtual void OnWindowCloseRequested() { }
    
    #endregion
    
    #region Keyboard events

    public virtual void OnKeyDown(uint keyboardId, Keycode key, Keymod mod, bool repeat) { }
    
    public virtual void OnKeyUp(uint keyboardId, Keycode key, Keymod mod, bool repeat) { }
    
    #endregion
    
    #region Text events

    public virtual void OnTextEditing(string text, int start, int length) { }

    public virtual void OnTextInput(string text) { }
    
    #endregion

    #region Mouse events

    /// <summary>
    /// Mouse motion event
    /// </summary>
    /// <param name="mouseId">The mouse instance id in relative mode</param>
    /// <param name="state">The current button state</param>
    /// <param name="x">X coordinate, relative to window</param>
    /// <param name="y">Y coordinate, relative to window</param>
    /// <param name="xrel">The relative motion in the X direction</param>
    /// <param name="yrel">The relative motion in the Y direction</param>
    public virtual void OnMouseMotion(uint mouseId, MouseButtonFlags state, float x, float y, float xrel, float yrel) { }
    
    public virtual void OnMouseButtonDown(uint mouseId, MouseButton button, byte clicks, float x, float y) { }
    
    public virtual void OnMouseButtonUp(uint mouseId, MouseButton button, byte clicks, float x, float y) { }
    
    /// <summary>
    /// Mouse wheel motion
    /// </summary>
    /// <param name="mouseId">The mouse instance id in relative mode</param>
    /// <param name="x">The amount scrolled horizontally, positive to the right and negative to the left</param>
    /// <param name="y">The amount scrolled vertically, positive away from the user and negative toward the user</param>
    /// <param name="direction">When FLIPPED the values in X and Y will be opposite. Multiply by -1 to change them back</param>
    /// <param name="mouseX">X coordinate, relative to window</param>
    /// <param name="mouseY">Y coordinate, relative to window</param>
    public virtual void OnMouseWheel(uint mouseId, float x, float y, MouseWheelDirection direction, float mouseX, float mouseY) { }
    
    #endregion

    #region Gamepad events

    public virtual void OnGamepadAxisMotion(uint gamepadId, GamepadAxis axis, short value) { }

    public virtual void OnGamepadButtonDown(uint gamepadId, GamepadButton button) { }
    
    public virtual void OnGamepadButtonUp(uint gamepadId, GamepadButton button) { }
    
    public virtual void OnGamepadTouchpadDown(uint gamepadId, int touchpad, int finger, float x, float y, float pressure) { }
    
    public virtual void OnGamepadTouchpadMotion(uint gamepadId, int touchpad, int finger, float x, float y, float pressure) { }
    
    public virtual void OnGamepadTouchpadUp(uint gamepadId, int touchpad, int finger, float x, float y, float pressure) { }

    public virtual void OnGamepadSensorUpdate(uint gamepadId, int sensor, float[] data) { }
    
    #endregion
    
    #region Sensor events

    public virtual void OnFingerDown(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }
    
    public virtual void OnFingerUp(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }
    
    public virtual void OnFingerMotion(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }
    
    public virtual void OnFingerCanceled(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }

    public virtual void OnSensorUpdate(uint sensorId, float[] data) { }
    
    #endregion
    
    public virtual void OnQuit() { }
}
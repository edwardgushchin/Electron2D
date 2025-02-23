using Electron2D.Input;

namespace Electron2D;

public abstract class Scene
{ 
    private Color _clearColor = Color.Black;
    
    private List<GameObject> GameObjects { get; } = [];
    
    internal IRenderContext? RenderContext { get; set; }
    
    public Color ClearColor
    {
        get => _clearColor;
        set
        {
            _clearColor = value;
            RenderContext?.SetClearColor(_clearColor);
        }
    }
    
    
    public void AddGameObject(GameObject gameObject)
    {
        GameObjects.Add(gameObject);
    }
    
    public void RemoveGameObject(GameObject gameObject)
    {
        GameObjects.Remove(gameObject);
    }

    public GameObject? FindGameObject(string name)
    {
        return GameObjects.FirstOrDefault(obj => obj.Name == name);
    }
    
    
    public void InternalUpdate(float deltaTime)
    {
        Update(deltaTime);

        foreach (var obj in GameObjects)
        {
            obj.InternalUpdate(deltaTime);
        }

        LateUpdate();
    }

    public void InternalFixedUpdate(float fixedDeltaTime)
    {
        FixedUpdate(fixedDeltaTime);
    }
    
    /// <summary>
    /// Отрисовывает все игровые объекты сцены.
    /// </summary>
    internal void InternalRender()
    {
        if (RenderContext == null) return;

        SpriteRenderer.Render(RenderContext);
    }
    
    protected virtual void Awake() { }       // Вызывается при создании объекта (до Start)
    
    protected virtual void Start() { }       // Вызывается перед первым кадром

    internal void InternalAwake()
    {
        Awake();
        
        foreach (var obj in GameObjects)
        {
            obj.InternalAwake();
        }
    }
    
    internal void InternalStart()
    {
        Start();
        
        foreach (var obj in GameObjects)
        {
            obj.InternalStart();
        }
    }
    
    protected virtual void Update(float deltaTime) { }      // Вызывается каждый кадр
    
    protected virtual void FixedUpdate(float fixedDeltaTime) { } // Вызывается через фиксированные интервалы (физика)
    
    protected internal virtual void LateUpdate() { }  // Вызывается после Update (полезно для камеры)
    
    protected internal virtual void OnPreRender() { }   // Перед рендером камеры
    
    protected internal virtual void OnPostRender() { }  // После рендера камеры
    
    #region Window events
    
    /// <summary>
    /// Window has been shown
    /// </summary>
    protected internal virtual void OnWindowShown() { }

    /// <summary>
    /// Window has been hidden
    /// </summary>
    protected internal virtual void OnWindowHidden() { }
    
    /// <summary>
    /// Window has been moved
    /// </summary>
    /// <param name="x">New window position by X coordinate</param>
    /// <param name="y">New window position by Y coordinate</param>
    protected internal virtual void OnWindowMoved(int x, int y) { }

    protected internal virtual void OnWindowResized(int width, int height) { }
    
    protected internal virtual void OnWindowMinimized() { }
    
    protected internal virtual void OnWindowMaximized() { }
    
    protected internal virtual void OnWindowRestored() { }
    
    protected internal virtual void OnWindowFocusGained() { }
    
    protected internal virtual void OnWindowFocusLost() { }
    
    protected internal virtual void OnWindowCloseRequested() { }
    
    #endregion
    
    #region Keyboard events

    protected internal virtual void OnKeyDown(uint keyboardId, Keycode key, Keymod mod, bool repeat) { }
    
    protected internal virtual void OnKeyUp(uint keyboardId, Keycode key, Keymod mod, bool repeat) { }
    
    #endregion
    
    #region Text events

    protected internal virtual void OnTextEditing(string text, int start, int length) { }

    protected internal virtual void OnTextInput(string text) { }
    
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
    protected internal virtual void OnMouseMotion(uint mouseId, MouseButtonFlags state, float x, float y, float xrel, float yrel) { }
    
    protected internal virtual void OnMouseButtonDown(uint mouseId, MouseButton button, byte clicks, float x, float y) { }
    
    protected internal virtual void OnMouseButtonUp(uint mouseId, MouseButton button, byte clicks, float x, float y) { }
    
    /// <summary>
    /// Mouse wheel motion
    /// </summary>
    /// <param name="mouseId">The mouse instance id in relative mode</param>
    /// <param name="x">The amount scrolled horizontally, positive to the right and negative to the left</param>
    /// <param name="y">The amount scrolled vertically, positive away from the user and negative toward the user</param>
    /// <param name="direction">When FLIPPED the values in X and Y will be opposite. Multiply by -1 to change them back</param>
    /// <param name="mouseX">X coordinate, relative to window</param>
    /// <param name="mouseY">Y coordinate, relative to window</param>
    protected internal virtual void OnMouseWheel(uint mouseId, float x, float y, MouseWheelDirection direction, float mouseX, float mouseY) { }
    
    #endregion

    #region Gamepad events

    protected internal virtual void OnGamepadAxisMotion(uint gamepadId, GamepadAxis axis, short value) { }

    protected internal virtual void OnGamepadButtonDown(uint gamepadId, GamepadButton button) { }
    
    protected internal virtual void OnGamepadButtonUp(uint gamepadId, GamepadButton button) { }
    
    protected internal virtual void OnGamepadTouchpadDown(uint gamepadId, int touchpad, int finger, float x, float y, float pressure) { }
    
    protected internal virtual void OnGamepadTouchpadMotion(uint gamepadId, int touchpad, int finger, float x, float y, float pressure) { }
    
    protected internal virtual void OnGamepadTouchpadUp(uint gamepadId, int touchpad, int finger, float x, float y, float pressure) { }

    protected internal virtual void OnGamepadSensorUpdate(uint gamepadId, int sensor, float[] data) { }
    
    #endregion
    
    #region Sensor events

    protected internal virtual void OnFingerDown(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }
    
    protected internal virtual void OnFingerUp(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }
    
    protected internal virtual void OnFingerMotion(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }
    
    protected internal virtual void OnFingerCanceled(ulong touchId, ulong fingerId, float x, float y, float dx, float dy, float pressure) { }

    protected internal virtual void OnSensorUpdate(uint sensorId, float[] data) { }
    
    #endregion
    
    protected internal virtual void OnDestroy() { }  // Когда объект уничтожается

    internal void InternalDestroy()
    {
        OnDestroy();
        
        foreach (var obj in GameObjects)
        {
            obj.InternalOnDestroy();
        }
    }
    
    protected internal virtual void OnQuit() { }   // Перед выходом из игры
    protected internal virtual void OnPause(bool pause) { } // При паузе
}
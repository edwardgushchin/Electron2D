using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    //private CloseConfirmControl _confirm = null!;
    private const float _cameraSpeed = 6f;
    private Camera _camera = null!;
    
    
    protected override void Ready()
    {
        //_confirm = new CloseConfirmControl();
        _camera = new Camera("Main");
        
        AddChild(_camera);
        //AddChild(_confirm);
        AddChild(new Player());

        SceneTree!.Paused = true;

        //SceneTree!.OnQuitRequested.Connect(() => _confirm.Open());
        //SceneTree!.OnWindowCloseRequested.Connect(_ => _confirm.Open());
    }

    protected override void Process(float delta)
    {
        if(Input.IsKeyDown(KeyCode.Left)) _camera.Transform.TranslateX(-_cameraSpeed * delta);
        if(Input.IsKeyDown(KeyCode.Right)) _camera.Transform.TranslateX(_cameraSpeed * delta);
        
        if(Input.IsKeyDown(KeyCode.Up)) _camera.Transform.TranslateY(_cameraSpeed * delta);
        if(Input.IsKeyDown(KeyCode.Down)) _camera.Transform.TranslateY(-_cameraSpeed * delta);
        
        if(Input.IsKeyDown(KeyCode.Space)) _camera.Transform.RotateRight(_cameraSpeed * delta);
    }

    protected override void HandleUnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is { Type: InputEventType.KeyDown, Code: KeyCode.Escape })
        {
            //_confirm.Open();
            SceneTree?.Quit();
        }
    }
}
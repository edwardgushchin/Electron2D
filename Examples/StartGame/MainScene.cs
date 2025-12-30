using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    CloseConfirmControl confirm = null!;
    float _cameraSpeed = 10f;
    Camera? _camera;
    
    protected override void Ready()
    {
        confirm = new CloseConfirmControl();
        
        AddChild(confirm);
        AddChild(new Player());

        SceneTree!.OnQuitRequested.Connect(() => confirm.Open());
        SceneTree!.OnWindowCloseRequested.Connect(_ => confirm.Open());

        _camera = SceneTree?.CurrentCamera;
    }

    protected override void Process(float delta)
    {
        if (_camera != null)
        {
            if(Input.IsKeyDown(KeyCode.Left)) _camera.Transform.TranslateX(-_cameraSpeed * delta);
            if(Input.IsKeyDown(KeyCode.Right)) _camera.Transform.TranslateX(_cameraSpeed * delta);
        
            if(Input.IsKeyDown(KeyCode.Up)) _camera.Transform.TranslateY(_cameraSpeed * delta);
            if(Input.IsKeyDown(KeyCode.Down)) _camera.Transform.TranslateY(-_cameraSpeed * delta);
        
            if(Input.IsKeyDown(KeyCode.Space)) _camera.Transform.RotateRight(_cameraSpeed * delta);
        }
    }

    protected override void HandleUnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is { Type: InputEventType.KeyDown, Code: KeyCode.Escape })
        {
            confirm.Open();
        }
    }
}
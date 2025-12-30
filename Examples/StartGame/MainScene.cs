using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    CloseConfirmControl confirm = null!;
    
    protected override void Ready()
    {
        confirm = new CloseConfirmControl();
        
        AddChild(confirm);
        AddChild(new Player());

        SceneTree!.OnQuitRequested.Connect(() => confirm.Open());
        SceneTree!.OnWindowCloseRequested.Connect(_ => confirm.Open());
    }

    protected override void HandleUnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is { Type: InputEventType.KeyDown, Code: (int)KeyCode.Escape })
        {
            confirm.Open();
        }
    }
}
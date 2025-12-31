using System.Numerics;
using Electron2D;

namespace StartGame;

public class MainScene() : Node("MainScene")
{
    //private CloseConfirmControl _confirm = null!;

    protected override void EnterTree()
    {
        SceneTree?.ClearColor = new Color(47, 47, 56);
    }


    protected override void Ready()
    {
        //_confirm = new CloseConfirmControl();
        
        AddChild(new DebugCamera());
        //AddChild(_confirm);
        AddChild(new Player());

        SceneTree!.Paused = true;

        //SceneTree!.OnQuitRequested.Connect(() => _confirm.Open());
        //SceneTree!.OnWindowCloseRequested.Connect(_ => _confirm.Open());
    }

    protected override void Process(float delta)
    {
        if ((Profiler.LastFrame.FrameIndex % 60) == 0)
            Console.WriteLine(Profiler.LastFrame);
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
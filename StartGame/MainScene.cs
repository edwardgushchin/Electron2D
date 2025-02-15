using Electron2D;
using Electron2D.Input;

namespace StartGame;

public class MainScene : Scene
{
    public override void OnStart()
    {
        
    }

    public override void OnLoad()
    {
        
    }

    public override void Update(float deltaTime)
    {
        
    }

    public override void Render()
    {
    }

    public override void Shutdown()
    {
    }

    public override void OnKeyDown(uint id, Keycode key, Keymod mod, bool repeat)
    {
        if(key == Keycode.Escape) Kernel.Exit();
    }


    public override void OnQuit()
    {
        Kernel.Exit();
    }
}
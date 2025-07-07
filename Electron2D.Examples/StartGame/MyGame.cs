using Electron2D;
using Electron2D.Components;
using Electron2D.Inputs;
using Electron2D.Resources;

namespace StartGame;

public class MyGame() : Game("Electron 2D Game Engine Demo")
{
    protected override void Initialize()
    {
        var playerTexture = ResourceManager.LoadTexture("hero", Path.Combine("assets", "hero.png"));
        
        RootNode.AddChild(new Player("hero", playerTexture));
    }

    /*protected override void Update(float deltaTime)
    {
        if(Input.GetKeyDown(Scancode.Escape))
            Exit();
        
        if(Input.GetKeyDown(Scancode.KpPlus))
            Camera.ActiveCamera!.Zoom += 0.1f * deltaTime;
        
        if(Input.GetKeyDown(Scancode.KpMinus))
            Camera.ActiveCamera!.Zoom -= 0.1f * deltaTime;
        
        if(Input.GetKeyDown(Scancode.A))
            Camera.ActiveCamera!.Transform.LocalPosition -= new Vector2(deltaTime, 0);
        
        if(Input.GetKeyDown(Scancode.D))
            Camera.ActiveCamera!.Transform.LocalPosition += new Vector2(deltaTime, 0);
        
        if(Input.GetKeyDown(Scancode.W))
            Camera.ActiveCamera!.Transform.LocalPosition += new Vector2(0, deltaTime);
        
        if(Input.GetKeyDown(Scancode.S))
            Camera.ActiveCamera!.Transform.LocalPosition -= new Vector2(0, deltaTime);
    }*/
}

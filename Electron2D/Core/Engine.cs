namespace Electron2D;

public class Engine
{
    public SceneTree SceneTree { get; set; }

    public Engine()
    {
        SceneTree = new SceneTree(new Node("root"));
    }
}
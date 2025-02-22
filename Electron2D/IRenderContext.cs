namespace Electron2D;

public interface IRenderContext
{
    void SetClearColor(Color color);
    
    Color GetClearColor();
}
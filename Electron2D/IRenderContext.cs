namespace Electron2D;

public interface IRenderContext
{
    void SetClearColor(Color color);
    
    void RenderTexture(Texture texture, Vector2 size, Vector3 position);
    
    Color GetClearColor();
}
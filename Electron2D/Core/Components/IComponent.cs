namespace Electron2D;

public interface IComponent
{
    void OnAttach(Node owner);
    void OnDetach();
}
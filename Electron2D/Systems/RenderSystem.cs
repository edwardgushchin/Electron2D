using Electron2D.Graphics;
using Electron2D.Physics;
using SDL3;

namespace Electron2D.Systems;

internal class RenderSystem(Render renderer)
{
    private readonly List<Sprite> _renderQueue = [];
    private readonly List<Collider> _colliderQueue = []; // Add this

    // Собирает все спрайты из дерева
    public void CollectSprites(Node root)
    {
        _renderQueue.Clear();
        _colliderQueue.Clear(); // Add this
        Traverse(root);
    }

    private void Traverse(Node node)
    {
        if (node is Sprite { IsEnabled: true } sprite)
            _renderQueue.Add(sprite);
        
        if (node is Collider { IsEnabled: true, ShowDebugOutline: true } collider)
            _colliderQueue.Add(collider);

        foreach (var child in node.GetChildren())
            Traverse(child);
    }

    public void RenderAll()
    {
        // Сортируем по слою
        var needsSort = false;

        foreach (var sprite in _renderQueue)
        {
            if (!sprite.LayerDirty) continue;
            needsSort = true;
            break;
        }

        if (needsSort)
        {
            _renderQueue.Sort(static (a, b) => a.Layer.CompareTo(b.Layer));

            // Сбрасываем флаг
            foreach (var sprite in _renderQueue)
                sprite.LayerDirty = false;
        }
        
        SDL.SetRenderDrawColor(renderer.Handle, 0, 0, 0, 255);
        SDL.RenderClear(renderer.Handle);

        foreach (var sprite in _renderQueue)
        {
            renderer.DrawSprite(sprite);
        }
        
        foreach (var collider in _colliderQueue)
        {
            renderer.DrawColliderDebug(collider);
        }
        
        SDL.RenderPresent(renderer.Handle);
    }
}
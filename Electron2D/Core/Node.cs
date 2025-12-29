namespace Electron2D;

public class Node
{
    /// <summary>
    /// Этот сигнал генерируется, когда дочерний узел входит в дерево сцены, обычно потому, что этот узел вошел в дерево
    /// </summary>
    public Signal<Node> OnChildEnteredTree = new();
    
    /// <summary>
    /// тот сигнал генерируется, когда дочерний узел собирается покинуть дерево сцен, обычно потому, что этот узел выходит из дерева, или потому, что дочерний узел удаляется или освобождается.
    /// </summary>
    public Signal<Node> OnChildExitingTree = new();
    
    /// <summary>
    /// Сообщения генерируются при изменении списка дочерних узлов. Это происходит при добавлении, перемещении или удалении дочерних узлов.
    /// </summary>
    public Signal OnChildOrderChanged = new();
    
    /// <summary>
    /// 
    /// </summary>
    public Signal OnEntered = new();
    
    /// <summary>
    /// 
    /// </summary>
    public Signal OnExiting = new();
    
    /// <summary>
    /// 
    /// </summary>
    public Signal OnExited = new();
    
    /// <summary>
    /// Генерируется, когда узел считается готовым, после вызова функции <see cref="Ready"/>.
    /// </summary>
    public Signal OnReady = new();
    
    /// <summary>
    /// Сообщение генерируется при изменении имени узла, если узел находится внутри дерева.
    /// </summary>
    public Signal OnRenamed = new();
    
    
    protected virtual void EnterTree()
    {
        
    }
    
    protected virtual void Ready()
    {
    }

    protected virtual void Process(float delta)
    {
        
    }
    
    protected virtual void PhysicsProcess(float fixedDelta)
    {
        
    }
    
    protected virtual void ExitTree()
    {
    }
    
    public void AddChild(Node child)
    {
        
    }

    public void RemoveChild(Node child)
    {
        
    }

    public Node GetChild(int index)
    {
        throw new NotImplementedException();
    }

    public int GetChildCount()
    {
        throw new NotImplementedException();
    }

    public Node GetParent()
    {
        throw new NotImplementedException();
    }

    public int GetIntex()
    {
        throw new NotImplementedException();
    }

    public Node GetNode(string path)
    {
        throw new NotImplementedException();
    }

    public Node? GetNodeOrNull(string path)
    {
        throw new NotImplementedException();
    }

    public bool HasNode(string path)
    {
        throw new NotImplementedException();
    }
}
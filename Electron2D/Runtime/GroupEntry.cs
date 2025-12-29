namespace Electron2D;

struct GroupEntry
{
    public string Name;
    public bool Persistent;
    public int TreeIndex; // -1 если не зарегистрирован в SceneTree-индексе

    public GroupEntry(string name, bool persistent)
    {
        Name = name;
        Persistent = persistent;
        TreeIndex = -1;
    }
}
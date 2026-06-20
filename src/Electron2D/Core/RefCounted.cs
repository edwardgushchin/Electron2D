namespace Electron2D;

public class RefCounted : Object
{
    private int _referenceCount = 1;

    public bool Reference()
    {
        if (!IsInstanceValid(this) || _referenceCount <= 0)
        {
            return false;
        }

        checked
        {
            _referenceCount++;
        }

        return true;
    }

    public bool Unreference()
    {
        if (_referenceCount <= 0)
        {
            return true;
        }

        _referenceCount--;
        if (_referenceCount == 0)
        {
            Free();
            return true;
        }

        return false;
    }

    public int GetReferenceCount()
    {
        return _referenceCount;
    }

    protected override void OnFree()
    {
        _referenceCount = 0;
        base.OnFree();
    }
}

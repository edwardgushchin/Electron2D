using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed class Signal(int capacity = 8)
{
    private Action[] _slots = new Action[capacity];
    private int _count;

    public void Connect(Action handler)
    {
        if (_count == _slots.Length)
        {
            Array.Resize(ref _slots, _slots.Length * 2);
        }

        _slots[_count++] = handler;
    }

    public void Disconnect(Action handler)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_slots[i] != handler)
            {
                continue;
            }

            _count--;
            _slots[i] = _slots[_count];
            _slots[_count] = null!;
            return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit()
    {
        for (var i = 0; i < _count; i++)
        {
            _slots[i]();
        }
    }
}

public sealed class Signal<T>(int capacity = 8)
{
    private Action<T>[] _slots = new Action<T>[capacity];
    
    private int _count;

    public void Connect(Action<T> handler)
    {
        // без аллокаций при кадре, аллокация — только когда расширяем массив
        if (_count == _slots.Length)
        {
            Array.Resize(ref _slots, _slots.Length * 2);
        }

        _slots[_count++] = handler;
    }
    
    public void Disconnect(Action<T> handler)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_slots[i] == handler)
            {
                _count--;
                _slots[i] = _slots[_count];
                _slots[_count] = null!;
                return;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Emit(T arg)
    {
        // обычный for, без IEnumerable/foreach
        for (int i = 0; i < _count; i++)
            _slots[i](arg);
    }
}
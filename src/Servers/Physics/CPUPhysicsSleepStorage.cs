using System.Numerics;
using Box2D.NET;

namespace Electron2D;

/// <summary>Reuses dormant solver records across changing island partitions without per-island maximum copies.</summary>
internal sealed class CPUPhysicsSleepStorage
{
    private readonly Buffers<B2BodySim> _bodies = new();
    private readonly Buffers<B2ContactSim> _contacts = new();
    private readonly Buffers<B2JointSim> _joints = new();
    private readonly Buffers<B2IslandSim> _islands = new();

    internal void Reserve(int bodies, int contacts, int joints)
    {
        _bodies.Reserve(bodies, bodies); _contacts.Reserve(contacts, bodies);
        _joints.Reserve(joints, bodies); _islands.Reserve(bodies, bodies);
    }
    internal void Prepare(B2SolverSet set, int bodies, int contacts, int joints, int islands)
    {
        _bodies.Ensure(ref set.bodySims, bodies); _contacts.Ensure(ref set.contactSims, contacts);
        _joints.Ensure(ref set.jointSims, joints); _islands.Ensure(ref set.islandSims, islands);
    }
    internal void Release(B2SolverSet set)
    {
        _bodies.Release(ref set.bodySims); _contacts.Release(ref set.contactSims);
        _joints.Release(ref set.jointSims); _islands.Release(ref set.islandSims);
    }
    internal void Clear() { _bodies.Clear(); _contacts.Clear(); _joints.Clear(); _islands.Clear(); }

    private sealed class Buffers<T> where T : class, new()
    {
        private T[][][] _free = new T[31][][];
        private int[] _counts = new int[31], _created = new int[31];
        private T[] _objects = [];
        private int _objectCount, _objectCapacity;

        internal void Reserve(int budget, int slots)
        {
            // Current and restored rounded partitions can coexist while destinations are prepared.
            var objectCapacity = checked(budget * 4);
            if (objectCapacity > _objectCapacity)
            {
                Array.Resize(ref _objects, objectCapacity);
                for (var i = _objectCapacity; i < objectCapacity; i++) _objects[_objectCount++] = new();
                _objectCapacity = objectCapacity;
            }
            var maximum = BitOperations.RoundUpToPowerOf2((uint)budget);
            for (var level = 0; (1L << level) <= maximum; level++)
            {
                var size = 1 << level;
                // Current and saved partitions can hold different buckets until restore commits.
                var count = Math.Min(slots, checked(budget * 2) / (size == 1 ? 1 : size / 2 + 1));
                if (count <= _created[level]) continue;
                Array.Resize(ref _free[level], count);
                for (var i = _created[level]; i < count; i++) _free[level][_counts[level]++] = new T[size];
                _created[level] = count;
            }
        }
        internal void Ensure(ref B2Array<T> array, int count)
        {
            if (array.capacity >= count) return;
            var size = checked((int)BitOperations.RoundUpToPowerOf2((uint)count));
            var level = BitOperations.Log2((uint)size);
            T[] data;
            if (_counts[level] > 0)
            {
                data = _free[level][--_counts[level]]; _free[level][_counts[level]] = null!;
            }
            else data = new T[size];
            for (var i = 0; i < array.count; i++) { data[i] = array.data[i]; array.data[i] = null!; }
            for (var i = array.count; i < size; i++)
            {
                if (_objectCount == 0) data[i] = new();
                else { data[i] = _objects[--_objectCount]; _objects[_objectCount] = null!; }
            }
            Return(array.data); array.data = data; array.capacity = size;
        }
        internal void Release(ref B2Array<T> array) { Return(array.data); array = default; }
        private void Return(T[]? data)
        {
            if (data is null) return;
            foreach (var item in data)
                if (item is not null && _objectCount < _objects.Length) _objects[_objectCount++] = item;
            Array.Clear(data);
            if (data.Length == 0 || !BitOperations.IsPow2(data.Length)) return;
            var level = BitOperations.Log2((uint)data.Length);
            if (_free[level] is { } bucket && _counts[level] < bucket.Length) bucket[_counts[level]++] = data;
        }
        internal void Clear() { _free = []; _counts = []; _created = []; _objects = []; _objectCount = _objectCapacity = 0; }
    }
}

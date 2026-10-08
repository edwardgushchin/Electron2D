using Box2D.NET;
using static Box2D.NET.B2Contacts;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    private readonly Storage<B2ContactRemoval> _contactRemovalStorage;
    private readonly Action<B2World, B2Contact> _destroyDisjointContact;
    private readonly Action<B2World> _finishContactRemovals;
    private int _removalCount, _removalCursor, _removalVersion, _releasedContactID = -1;
    private int[] _removalStamps = [];
    private bool _removalsReady;
    internal long RemovedContactCount { get; private set; }
    internal long ContactRemovalReadbackBytes { get; private set; }

    private void BeginContactRemovals()
    {
        if (_removalsReady) throw new InvalidOperationException("The preceding GPU contact removal batch was not published.");
        _removalCount = _removalCursor = 0;
        if (_removalStamps.Length < _contactSlotStorage.Data.Length) Array.Resize(ref _removalStamps, _contactSlotStorage.Data.Length);
        if (_removalVersion == int.MaxValue) { Array.Clear(_removalStamps); _removalVersion = 0; }
        _removalVersion++;
    }

    private void RecordContactRemovals(nint command, B2World world, int pairCount)
    {
        if (_removalCount == 0) return;
        ContactCreationPass(command, 13, pairCount);
        _contactLeafBase = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)world.contactIdPool.nextIndex));
        ContactCreationPass(command, 14, world.contactIdPool.nextIndex, dispatch: _contactLeafBase);
        for (var width = _contactLeafBase / 2; width > 0; width /= 2)
            ContactCreationPass(command, 4, 0, offset: width, width: width, dispatch: width);
        ContactCreationPass(command, 15, _removalCount, dispatch: 1);
        ContactCreationPass(command, 16, world.contactIdPool.nextIndex);
        _contactLeafBase = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)_removalCount));
        var endpoints = 2 * _contactLeafBase;
        ContactCreationPass(command, 17, _removalCount, dispatch: endpoints);
        SortContactEndpoints(command, endpoints);
        ContactCreationPass(command, 18, endpoints);
        ContactCreationPass(command, 19, _removalCount);
    }

    private void ReadContactRemovals(B2World world)
    {
        if (_removalCount == 0) return;
        _contactRemovalStorage.Read(_removalCount);
        if (_contactPoolStorage.Data[0].OperationCount != _removalCount)
            throw new InvalidOperationException("GPU contact removal count differs.");
        var lastID = -1;
        for (var i = 0; i < _removalCount; i++)
        {
            ref readonly var removal = ref _contactRemovalStorage.Data[i];
            var id = removal.ID;
            if (id <= lastID || (uint)id >= (uint)world.contacts.count || _removalStamps[id] != _removalVersion)
                throw new InvalidOperationException("GPU contact removal returned a stale or unordered identity.");
            var contact = world.contacts.data[id];
            if (contact.contactId != id || removal.Generation != contact.generation ||
                removal.BodyA != contact.edges[0].bodyId || removal.BodyB != contact.edges[1].bodyId ||
                removal.CountA < 0 || removal.CountB < 0 ||
                !ValidRemovalKey(removal.PrevA) || !ValidRemovalKey(removal.NextA) || !ValidRemovalKey(removal.HeadA) ||
                !ValidRemovalKey(removal.PrevB) || !ValidRemovalKey(removal.NextB) || !ValidRemovalKey(removal.HeadB))
                throw new InvalidOperationException("GPU contact removal returned invalid body links.");
            lastID = id;
        }
        _removalsReady = true;
    }

    private bool ValidRemovalKey(int key) => key == -1 || key >= 0 && (key >> 1) < _contactPoolStorage.Data[0].Next;

    private void PublishContactRemoval(B2World world, B2Contact contact)
    {
        EnsureOwner();
        if (!_removalsReady || !ReferenceEquals(_contactTrackingWorld, world) || _removalCursor >= _removalCount ||
            _contactRemovalStorage.Data[_removalCursor].ID != contact.contactId)
            throw new InvalidOperationException("GPU contact removal publication is out of order.");
        try
        {
            _publishingContactLinks = true; _releasedContactID = contact.contactId;
            b2DestroyContactPrepared(world, contact, in _contactRemovalStorage.Data[_removalCursor]);
            _removalCursor++;
        }
        finally { _publishingContactLinks = false; _releasedContactID = -1; }
    }

    private void FinishContactRemovals(B2World world)
    {
        if (_removalCount == 0) return;
        if (!_removalsReady || _removalCursor != _removalCount)
            throw new InvalidOperationException("GPU contact removal publication is incomplete.");
        ValidateContactPool(world.contactIdPool.nextIndex, world.contactIdPool.freeArray.count);
        RemovedContactCount += _removalCount;
        _removalsReady = false; _removalCount = _removalCursor = 0;
    }
}

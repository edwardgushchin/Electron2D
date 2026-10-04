using System.Buffers.Binary;
using System.Text;
namespace Electron2D;

public partial class SceneMultiplayer
{
    private readonly Dictionary<Node, SpawnedNodeRecord> _localSpawns = [];
    private readonly Dictionary<(int Source, uint ID), SpawnedNodeRecord> _remoteSpawns = [];
    private readonly Dictionary<MultiplayerSynchronizer, Node> _synchronizers = [];
    private readonly List<SpawnedNodeRecord> _spawnSnapshot = [];
    private readonly List<MultiplayerSynchronizer> _syncSnapshot = [];
    private readonly List<int> _replicationPeers = [];
    private readonly List<MultiplayerSynchronizer> _receiptGroups = [];
    internal bool ReplicationProcessing => _replicationFrame;
    private uint _nextSpawnID, _stateSequence;
    private bool _resettingReplication, _replicationFrame;
    private int _maxSync = 1350, _maxDelta = 65535;
    /// <summary>Gets or sets the maximum whole encoded periodic synchronization packet size.</summary><value>1350 initially; at least 128 bytes, bounded by 64 MiB and actual prepared/transport capacity.</value>
    public int MaxSyncPacketSize { get { CheckMultiplayer(); return _maxSync; } set { CheckMultiplayer(); if (value is < 128 or > 67108864) throw new ArgumentOutOfRangeException(nameof(value)); _maxSync = value; } }
    /// <summary>Gets or sets the maximum whole encoded change-only synchronization packet size.</summary><value>65535 initially; at least 128 bytes. Complete synchronizer groups are batched without splitting their properties.</value>
    public int MaxDeltaPacketSize { get { CheckMultiplayer(); return _maxDelta; } set { CheckMultiplayer(); if (value is < 128 or > 67108864) throw new ArgumentOutOfRangeException(nameof(value)); _maxDelta = value; } }
    /// <inheritdoc />
    public override void ObjectConfigurationAdd(Node node, MultiplayerSpawner spawner)
    {
        CheckMultiplayer(); ArgumentNullException.ThrowIfNull(node); ArgumentNullException.ThrowIfNull(spawner); if (spawner.Tree is null || !ReferenceEquals(spawner.Multiplayer, this)) throw new ArgumentException("Spawner must belong to this interface."); if (_localSpawns.ContainsKey(node)) return;
        var record = spawner.GetRecord(node); if (record.RemoteSource != 0) return; if (_nextSpawnID == uint.MaxValue) throw new InvalidOperationException("Spawn wire identities are exhausted."); record.ID = ++_nextSpawnID; record.Visible.Clear(); _localSpawns.Add(node, record); _spawnSnapshot.EnsureCapacity(_localSpawns.Count);
    }
    /// <inheritdoc />
    public override void ObjectConfigurationRemove(Node node, MultiplayerSpawner spawner)
    {
        CheckMultiplayer(); if (!_localSpawns.TryGetValue(node, out var record) || !ReferenceEquals(record.Spawner, spawner)) return; _localSpawns.Remove(node); List<Exception>? errors = null;
        foreach (var peer in record.Visible.ToArray()) try { SendDespawn(record, peer); } catch (Exception e) { (errors ??= []).Add(e); }
        record.Visible.Clear(); if (errors is not null) throw new AggregateException(errors);
    }
    /// <inheritdoc />
    public override void ObjectConfigurationAdd(Node node, MultiplayerSynchronizer synchronizer)
    {
        CheckMultiplayer(); ArgumentNullException.ThrowIfNull(node); ArgumentNullException.ThrowIfNull(synchronizer); if (!ReferenceEquals(node.Tree, AttachedTree) || !ReferenceEquals(synchronizer.Multiplayer, this)) throw new ArgumentException("Synchronization must belong to this interface's scene."); _synchronizers[synchronizer] = node; _syncSnapshot.EnsureCapacity(_synchronizers.Count); _receiptGroups.EnsureCapacity(_synchronizers.Count);
    }
    /// <inheritdoc />
    public override void ObjectConfigurationRemove(Node node, MultiplayerSynchronizer synchronizer) { CheckMultiplayer(); if (_synchronizers.TryGetValue(synchronizer, out var current) && ReferenceEquals(current, node)) _synchronizers.Remove(synchronizer); }
    internal void PrepareReplicationPeers(MultiplayerSynchronizer synchronizer) { foreach (var peer in _connected) synchronizer.PeerState(peer); }
    private void ReplicationPeerAdded(int id) { _replicationPeers.EnsureCapacity(_connected.Count); foreach (var sync in _synchronizers.Keys) sync.PeerState(id); }
    private void ReplicationPeerRemoved(int id)
    {
        foreach (var record in _localSpawns.Values) record.Visible.Remove(id); foreach (var sync in _synchronizers.Keys) sync.Peers.Remove(id);
        List<Exception>? errors = null; foreach (var entry in _remoteSpawns.ToArray()) if (entry.Key.Source == id) { _remoteSpawns.Remove(entry.Key); try { DeleteRemote(entry.Value); } catch (Exception e) { (errors ??= []).Add(e); } }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void ResetReplicationNetwork()
    {
        if (_resettingReplication) return; _resettingReplication = true; List<Exception>? errors = null;
        try { foreach (var record in _localSpawns.Values) record.Visible.Clear(); foreach (var sync in _synchronizers.Keys) sync.Peers.Clear(); foreach (var entry in _remoteSpawns.ToArray()) { _remoteSpawns.Remove(entry.Key); try { DeleteRemote(entry.Value); } catch (Exception e) { (errors ??= []).Add(e); } } }
        finally { _resettingReplication = false; }
        if (errors is not null) throw new AggregateException(errors);
    }
    private static void DeleteRemote(SpawnedNodeRecord record)
    { try { if (!record.Node.IsDisposed) { record.Spawner.EmitDespawned(record.Node); } } finally { if (!record.Node.IsDisposed) record.Node.Dispose(); } }
    internal void SpawnReady(SpawnedNodeRecord record) { if (record.Ready && !_sending && !_resettingReplication && _peer is not null && _peer.GetConnectionStatus() == MultiplayerConnectionStatus.Connected) RefreshSpawnVisibility(record, 0); }
    internal void UpdateReplicationVisibility(MultiplayerSynchronizer synchronizer, int peer)
    {
        CheckMultiplayer(); if (_sending || _resettingReplication || !_synchronizers.ContainsKey(synchronizer)) return;
        if (synchronizer.RootNode is { } root && _localSpawns.TryGetValue(root, out var record) && record.Ready) RefreshSpawnVisibility(record, peer);
    }
    private bool SpawnVisible(SpawnedNodeRecord record, int peer)
    {
        var any = false; foreach (var sync in _synchronizers) if (ReferenceEquals(sync.Value, record.Node) && sync.Key.IsMultiplayerAuthority()) { any = true; if (sync.Key.IsVisibleTo(peer)) return true; }
        return !any;
    }
    private void RefreshSpawnVisibility(SpawnedNodeRecord record, int peer)
    {
        if (!record.Spawner.IsMultiplayerAuthority() || !record.Ready) return; List<Exception>? errors = null;
        if (peer != 0) { if (!_connected.Contains(peer)) return; RefreshSpawnPeer(record, peer); return; }
        foreach (var id in _connected.ToArray()) try { RefreshSpawnPeer(record, id); } catch (Exception e) { (errors ??= []).Add(e); }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void RefreshSpawnPeer(SpawnedNodeRecord record, int peer)
    {
        var visible = SpawnVisible(record, peer); if (visible == record.Visible.Contains(peer)) return;
        if (visible) { SendSpawn(record, peer); record.Visible.Add(peer); } else { SendDespawn(record, peer); record.Visible.Remove(peer); }
    }
    private void SendDespawn(SpawnedNodeRecord record, int peer)
    { if (_resettingReplication || !_connected.Contains(peer) || _peer?.GetConnectionStatus() != MultiplayerConnectionStatus.Connected) return; Span<byte> packet = stackalloc byte[5]; packet[0] = 5; BinaryPrimitives.WriteUInt32LittleEndian(packet[1..], record.ID); SendCommand(peer, packet, TransferMode.Reliable, 0); }
    private void SendSpawn(SpawnedNodeRecord record, int peer)
    {
        BeginSend(); try
        {
            var path = PathFor(record.Spawner); var name = Encoding.UTF8.GetBytes(record.Node.Name); var count = 0;
            var header = checked(1 + 4 + 2 + path.Length + 4 + 4 + 2 + name.Length + 4 + record.Arguments.Length + 2); EnsureWrite(header, _send.Length);
            _send[count++] = 4; Put32(record.ID, ref count); PutPath(path, ref count); Put32(unchecked((uint)record.Scene), ref count); Put32(record.Factory, ref count); PutPath(name, ref count); Put32((uint)record.Arguments.Length, ref count); record.Arguments.CopyTo(_send.AsSpan(count)); count += record.Arguments.Length; var groupCount = count; count += 2; ushort groups = 0;
            foreach (var sync in _synchronizers.Keys.ToArray()) if (ReferenceEquals(sync.RootNode, record.Node))
                {
                    sync.Prepare(); var relative = Encoding.UTF8.GetBytes(record.Node.GetPathTo(sync)); PutPath(relative, ref count); var propertyCount = count; EnsureWrite(count + 2, _send.Length); count += 2; ushort properties = 0;
                    foreach (var binding in sync.Bindings) if (binding.Spawn) { binding.Capture(); WriteProperty(binding, ref count, _send.Length); properties++; }
                    BinaryPrimitives.WriteUInt16LittleEndian(_send.AsSpan(propertyCount), properties); groups++;
                }
            BinaryPrimitives.WriteUInt16LittleEndian(_send.AsSpan(groupCount), groups); SendCommand(peer, _send.AsSpan(0, count), TransferMode.Reliable, 0);
        }
        finally { _sending = false; }
    }
    private static void EnsureWrite(int count, int capacity) { if (count < 0 || count > capacity) throw new ArgumentException("Replication payload exceeds its prepared/packet budget."); }
    private void Put32(uint value, ref int offset) { EnsureWrite(offset + 4, _send.Length); BinaryPrimitives.WriteUInt32LittleEndian(_send.AsSpan(offset), value); offset += 4; }
    private void PutPath(ReadOnlySpan<byte> path, ref int offset) { if (path.Length > 4096) throw new ArgumentException("Replication path/name exceeds 4096 UTF8 bytes."); EnsureWrite(offset + 2 + path.Length, _send.Length); BinaryPrimitives.WriteUInt16LittleEndian(_send.AsSpan(offset), (ushort)path.Length); offset += 2; path.CopyTo(_send.AsSpan(offset)); offset += path.Length; }
    private void WriteProperty(BoundReplicationProperty binding, ref int offset, int capacity) { EnsureWrite(offset + 8 + binding.Length, capacity); Put32(binding.Property.ID, ref offset); Put32((uint)binding.Length, ref offset); binding.Current.AsSpan(0, binding.Length).CopyTo(_send.AsSpan(offset)); offset += binding.Length; }
    private void ReplicationFrame()
    {
        if (_connected.Count == 0 || _sending || _resettingReplication) return; var generation = _generation; List<Exception>? errors = null;
        _replicationPeers.Clear(); foreach (var id in _connected) _replicationPeers.Add(id);
        _spawnSnapshot.Clear(); foreach (var record in _localSpawns.Values) _spawnSnapshot.Add(record);
        foreach (var record in _spawnSnapshot) if (generation == _generation && _localSpawns.ContainsKey(record.Node) && record.Ready && record.Spawner.IsMultiplayerAuthority()) try { for (var i = 0; i < _replicationPeers.Count; i++) if (_connected.Contains(_replicationPeers[i])) RefreshSpawnPeer(record, _replicationPeers[i]); } catch (Exception e) { (errors ??= []).Add(e); }
        if (generation != _generation) return; _syncSnapshot.Clear(); foreach (var sync in _synchronizers.Keys) _syncSnapshot.Add(sync); _replicationPeers.Clear(); foreach (var id in _connected) _replicationPeers.Add(id); var now = System.Diagnostics.Stopwatch.GetTimestamp(); _stateSequence++; _sending = true; _replicationFrame = true;
        try
        {
            foreach (var sync in _syncSnapshot) if (_synchronizers.ContainsKey(sync) && sync.IsMultiplayerAuthority()) try { sync.CaptureFrame(now); } catch (Exception e) { (errors ??= []).Add(e); }
            for (var i = 0; i < _replicationPeers.Count && generation == _generation; i++) { var peer = _replicationPeers[i]; if (!_connected.Contains(peer)) continue; try { SendStates(peer, false, now); } catch (Exception e) { (errors ??= []).Add(e); } try { SendStates(peer, true, now); } catch (Exception e) { (errors ??= []).Add(e); } }
        }
        finally { _sending = false; _replicationFrame = false; }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void SendStates(int peer, bool delta, long now)
    {
        var overhead = RelaySupported && !IsServer() && peer != 1 ? 6 : 0; var capacity = Math.Min(delta ? _maxDelta : _maxSync, Math.Min(_send.Length, RequirePeer().GetMaxPacketSize()) - overhead); if (capacity < 5) throw new ArgumentException("Transport cannot hold a synchronization header."); var offset = 5; _receiptGroups.Clear(); _send[0] = delta ? (byte)22 : (byte)6; BinaryPrimitives.WriteUInt32LittleEndian(_send.AsSpan(1), _stateSequence);
        foreach (var sync in _syncSnapshot)
        {
            if (!_synchronizers.ContainsKey(sync) || !sync.IsMultiplayerAuthority() || !sync.IsVisibleTo(peer) || sync.RootNode is null || _localSpawns.TryGetValue(sync.RootNode, out var spawn) && !spawn.Visible.Contains(peer)) continue;
            var state = sync.PeerState(peer); if (delta ? !sync.DeltaDue(state, now) : !sync.AlwaysDue) continue; var path = PathFor(sync); var length = 2 + path.Length + 2; var propertyCount = 0;
            foreach (var binding in sync.Bindings) if (Eligible(binding, state, delta)) { length = checked(length + 8 + binding.Length); propertyCount++; }
            if (propertyCount == 0) continue; EnsureWrite(length + 5, capacity);
            if (offset + length > capacity) { SendCommand(peer, _send.AsSpan(0, offset), delta ? TransferMode.Reliable : TransferMode.Unreliable, 0); if (delta) CommitDelta(peer, now); offset = 5; }
            PutPath(path, ref offset); BinaryPrimitives.WriteUInt16LittleEndian(_send.AsSpan(offset), (ushort)propertyCount); offset += 2;
            foreach (var binding in sync.Bindings) if (Eligible(binding, state, delta)) WriteProperty(binding, ref offset, capacity);
            if (delta) _receiptGroups.Add(sync);
        }
        if (offset > 5) { SendCommand(peer, _send.AsSpan(0, offset), delta ? TransferMode.Reliable : TransferMode.Unreliable, 0); if (delta) CommitDelta(peer, now); }
    }
    private void CommitDelta(int peer, long now)
    {
        foreach (var sync in _receiptGroups) { var state = sync.PeerState(peer); for (var i = 0; i < sync.Bindings.Length; i++) if (sync.Bindings[i].Mode == ReplicationMode.OnChange) state.Sent[i] = sync.Bindings[i].Revision; state.LastDelta = now; }
        _receiptGroups.Clear();
    }
    private static bool Eligible(BoundReplicationProperty binding, ReplicationPeerState state, bool delta)
    { if (!delta) return binding.Mode == ReplicationMode.Always; if (binding.Mode != ReplicationMode.OnChange) return false; return state.Sent[binding.Index] != binding.Revision; }
    private bool RPCVisible(Node node, int peer)
    {
        var any = false; foreach (var sync in _synchronizers) if (ReferenceEquals(sync.Value, node)) { any = true; break; }
        if (!any) return true;
        if (_localSpawns.TryGetValue(node, out var spawned)) return spawned.Visible.Contains(peer);
        foreach (var remote in _remoteSpawns.Values) if (ReferenceEquals(remote.Node, node) && remote.RemoteSource == peer) return true;
        foreach (var sync in _synchronizers) if (ReferenceEquals(sync.Value, node) && sync.Key.IsVisibleTo(peer)) return true; return false;
    }
    private void SendRPCCommand(Node node, int target, ReadOnlySpan<byte> packet, TransferMode mode, int channel)
    {
        if (target > 0) { if (!RPCVisible(node, target)) throw new InvalidOperationException("RPC target cannot see the synchronized node."); SendCommand(target, packet, mode, channel); return; }
        SnapshotSendPeers(); List<Exception>? errors = null; for (var i = 0; i < _sendPeers.Count; i++) { var peer = _sendPeers[i]; if (peer == -target || !_connected.Contains(peer) || !RPCVisible(node, peer)) continue; try { SendCommand(peer, packet, mode, channel); } catch (Exception error) { (errors ??= []).Add(error); } }
        if (errors is not null) throw new AggregateException(errors);
    }
    private void ReceiveReplication(int sender, ReadOnlySpan<byte> packet)
    { if (packet[0] == 4) ReceiveSpawn(sender, packet); else if (packet[0] == 5) ReceiveDespawn(sender, packet); else ReceiveStates(sender, packet); }
    private static uint Read32(ReadOnlySpan<byte> bytes, ref int offset) { if (offset < 0 || bytes.Length - offset < 4) throw new InvalidDataException("Truncated replication integer."); var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]); offset += 4; return value; }
    private static ReadOnlySpan<byte> ReadPath(ReadOnlySpan<byte> bytes, ref int offset)
    { if (offset < 0 || bytes.Length - offset < 2) throw new InvalidDataException("Truncated replication path."); var length = BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]); offset += 2; if (length is 0 or > 4096 || length > bytes.Length - offset) throw new InvalidDataException("Invalid replication path length."); var path = bytes.Slice(offset, length); offset += length; return path; }
    private static ReadOnlySpan<byte> ReadPayload(ReadOnlySpan<byte> bytes, ref int offset) { var length = Read32(bytes, ref offset); if (length > bytes.Length - offset) throw new InvalidDataException("Truncated replication payload."); var data = bytes.Slice(offset, (int)length); offset += (int)length; return data; }
    private Node ResolveReplicationPath(ReadOnlySpan<byte> path)
    {
        ulong hash = 14695981039346656037; foreach (var b in path) hash = unchecked((hash ^ b) * 1099511628211); if (_inPaths.TryGetValue(hash, out var cached) && path.SequenceEqual(cached.Path)) return cached.Node;
        var tree = AttachedTree ?? throw new InvalidOperationException("Replication needs an assigned scene."); var text = new UTF8Encoding(false, true).GetString(path); if (text.StartsWith('/') || text.Contains('\0')) throw new InvalidDataException("Replication paths must be relative."); var root = tree.Root.GetNode(_rootPath); var node = root.GetNode(text); if (!ReferenceEquals(node.Multiplayer, this) || !ReferenceEquals(root, node) && !root.IsAncestorOf(node)) throw new InvalidDataException("Replication path escapes its configured branch."); _inPaths[hash] = new(node, path.ToArray()); return node;
    }
    private void ReceiveSpawn(int sender, ReadOnlySpan<byte> packet)
    {
        var offset = 1; var id = Read32(packet, ref offset); var spawner = ResolveReplicationPath(ReadPath(packet, ref offset)) as MultiplayerSpawner ?? throw new InvalidDataException("Spawn path must select a spawner."); if (id == 0 || spawner.GetMultiplayerAuthority() != sender || _remoteSpawns.ContainsKey((sender, id))) throw new InvalidDataException("Untrusted or duplicate spawn identity."); var scene = unchecked((int)Read32(packet, ref offset)); var factory = Read32(packet, ref offset); if (scene < -1 || scene >= 255 || scene >= 0 && factory != 0 || scene == -1 && factory == 0) throw new InvalidDataException("Invalid spawn template/factory identity."); var name = new UTF8Encoding(false, true).GetString(ReadPath(packet, ref offset)); var arguments = ReadPayload(packet, ref offset); var node = spawner.InstantiateRemote(scene, factory, arguments); var inserted = false;
        try
        {
            node.Name = name; if (spawner.SpawnParent.HasNode(name)) throw new InvalidDataException("Remote spawn name collides with an existing child."); ApplySpawnProperties(node, packet, ref offset); if (offset != packet.Length) throw new InvalidDataException("Trailing spawn bytes."); var record = spawner.TrackRemote(node, sender, id, scene, factory, arguments.ToArray()); _remoteSpawns.Add((sender, id), record); inserted = true; spawner.SpawnParent.AddChild(node); spawner.EmitSpawned(node);
        }
        catch { if (!inserted || node.Parent is null) { _remoteSpawns.Remove((sender, id)); if (!node.IsDisposed) node.Dispose(); } throw; }
    }
    private void ApplySpawnProperties(Node node, ReadOnlySpan<byte> packet, ref int offset)
    {
        if (packet.Length - offset < 2) throw new InvalidDataException("Missing spawn-state groups."); var groups = BinaryPrimitives.ReadUInt16LittleEndian(packet[offset..]); offset += 2;
        for (var i = 0; i < groups; i++)
        {
            var path = new UTF8Encoding(false, true).GetString(ReadPath(packet, ref offset)); if (path.StartsWith('/')) throw new InvalidDataException("Spawn state path must be relative."); var sync = node.GetNode(path) as MultiplayerSynchronizer ?? throw new InvalidDataException("Spawn property group must select a synchronizer."); var root = sync.GetNode(sync.RootPath); if (!ReferenceEquals(root, node) || !node.IsAncestorOf(sync)) throw new InvalidDataException("Spawn property group escapes its root."); sync.PrepareDetached(root); StageProperties(sync, packet, ref offset); ApplyStaged(sync);
        }
    }
    private void ReceiveDespawn(int sender, ReadOnlySpan<byte> packet)
    { var offset = 1; var id = Read32(packet, ref offset); if (offset != packet.Length) throw new InvalidDataException("Trailing despawn bytes."); if (!_remoteSpawns.Remove((sender, id), out var record)) throw new InvalidDataException("Despawn identity is unknown."); if (record.Spawner.GetMultiplayerAuthority() != sender) { _remoteSpawns.Add((sender, id), record); throw new InvalidDataException("Despawn sender is not authority."); } DeleteRemote(record); }
    private void ReceiveStates(int sender, ReadOnlySpan<byte> packet)
    {
        var offset = 1; var sequence = Read32(packet, ref offset); var delta = packet[0] == 22;
        while (offset < packet.Length)
        {
            var sync = ResolveReplicationPath(ReadPath(packet, ref offset)) as MultiplayerSynchronizer ?? throw new InvalidDataException("Synchronization path must select a synchronizer."); if (!_synchronizers.ContainsKey(sync) || sync.GetMultiplayerAuthority() != sender) throw new InvalidDataException("Synchronization source is not authority."); sync.Prepare(); var state = sync.PeerState(sender); var accepted = delta || !state.HasSequence || unchecked((int)(sequence - state.LastSequence)) > 0;
            if (!accepted) { SkipProperties(packet, ref offset); continue; }
            StageProperties(sync, packet, ref offset, delta);
            if (!delta) { state.HasSequence = true; state.LastSequence = sequence; }
            ApplyStaged(sync); sync.EmitState(delta);
        }
    }
    private static void SkipProperties(ReadOnlySpan<byte> bytes, ref int offset)
    { if (bytes.Length - offset < 2) throw new InvalidDataException("Missing old-state property count."); var count = BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]); offset += 2; for (var i = 0; i < count; i++) { _ = Read32(bytes, ref offset); _ = ReadPayload(bytes, ref offset); } }
    private static void StageProperties(MultiplayerSynchronizer sync, ReadOnlySpan<byte> bytes, ref int offset, bool? delta = null)
    {
        foreach (var binding in sync.Bindings) binding.Staged = false; if (bytes.Length - offset < 2) throw new InvalidDataException("Missing property count."); var count = BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]); offset += 2;
        try
        {
            for (var i = 0; i < count; i++) { var id = Read32(bytes, ref offset); BoundReplicationProperty? found = null; foreach (var binding in sync.Bindings) if (binding.Property.ID == id) { found = binding; break; } if (found is null || found.Staged || (delta is null ? !found.Spawn : found.Mode != (delta.Value ? ReplicationMode.OnChange : ReplicationMode.Always))) throw new InvalidDataException("Unknown or duplicate replication property ID."); found.Decode(ReadPayload(bytes, ref offset)); }
        }
        catch { foreach (var binding in sync.Bindings) binding.Staged = false; throw; }
    }
    private static void ApplyStaged(MultiplayerSynchronizer sync) { var bindings = sync.Bindings; List<Exception>? errors = null; foreach (var binding in bindings) try { binding.Apply(); } catch (Exception e) { (errors ??= []).Add(e); } if (errors is not null) throw new AggregateException(errors); }
}

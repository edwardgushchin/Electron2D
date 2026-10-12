namespace Electron2D;

public sealed partial class Animation
{
    internal void NotifyFileSchemaChanged() { EmitChanged(); NotifyPropertyListChanged(); }
    private IEnumerable<PropertyDescriptor> GetFileProperties()
    {
        yield return new PropertyDescriptor<Animation, string[]>("TrackSchemas", a => a.GetFileSchemas(), (a, v) => a.SetFileSchemas(v), stored: true);
        yield return new PropertyDescriptor<Animation, byte[]>("Markers", a => a.GetFileMarkers(), (a, v) => a.SetFileMarkers(v), stored: true);
        for (var track = 0; track < _tracks.Count; track++)
        {
            var index = track; var prefix = $"Track[{track}].";
            yield return new PropertyDescriptor<Animation, string>(prefix + "Path", a => a.TrackGetPath(index), (a, v) => a.TrackSetPath(index, v), stored: true);
            yield return new PropertyDescriptor<Animation, bool>(prefix + "Enabled", a => a.TrackIsEnabled(index), (a, v) => a.TrackSetEnabled(index, v), stored: true);
            yield return new PropertyDescriptor<Animation, bool>(prefix + "Imported", a => a.TrackIsImported(index), (a, v) => a.TrackSetImported(index, v), stored: true);
            yield return new PropertyDescriptor<Animation, bool>(prefix + "LoopWrap", a => a.TrackGetInterpolationLoopWrap(index), (a, v) => a.TrackSetInterpolationLoopWrap(index, v), stored: true);
            yield return new PropertyDescriptor<Animation, InterpolationType>(prefix + "Interpolation", a => a.TrackGetInterpolationType(index), (a, v) => a.TrackSetInterpolationType(index, v), stored: true);
            if (_tracks[index].Kind == TrackType.Value)
                yield return new PropertyDescriptor<Animation, UpdateMode>(prefix + "Update", a => a.ValueTrackGetUpdateMode(index), (a, v) => a.ValueTrackSetUpdateMode(index, v), stored: true);
            yield return new PropertyDescriptor<Animation, double[]>(prefix + "Times", a => a.Get(index).Times.ToArray(), (a, v) => a.SetFileTimes(index, v), stored: true);
            yield return new PropertyDescriptor<Animation, double[]>(prefix + "Transitions", a => a.Get(index).Transitions.ToArray(), (a, v) => a.SetFileTransitions(index, v), stored: true);
            foreach (var property in _tracks[index].FileMetadataProperties(index)) yield return property;
            foreach (var property in _tracks[index].FileKeyProperties(index)) yield return property;
        }
    }
    private string[] GetFileSchemas()
    {
        EnsureFileBudget(-1, 0);
        var schemas = new string[_tracks.Count];
        for (var i = 0; i < schemas.Length; i++)
        {
            schemas[i] = _tracks[i].Kind switch
            {
                TrackType.Value or TrackType.Bezier => ((int)_tracks[i].Kind).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + ResourceFileTypes.AnimationBindingID(_tracks[i]),
                TrackType.Method => "5:" + _tracks[i].FileReceiverID,
                TrackType.Audio => "7:audio",
                TrackType.Animation => "8:nested",
                _ => throw new NotSupportedException("This animation track kind has no portable schema.")
            };
        }
        return schemas;
    }
    private void SetFileSchemas(string[] schemas)
    {
        ArgumentNullException.ThrowIfNull(schemas);
        if (schemas.Length > 4096) throw new InvalidDataException("Animation track count exceeds file budget.");
        var tracks = new AnimationTrack[schemas.Length];
        for (var i = 0; i < tracks.Length; i++) tracks[i] = ResourceFileTypes.CreateAnimationFileTrack(schemas[i]);
        _tracks.Clear(); _tracks.AddRange(tracks); RefreshCaptureIncluded(); EmitChanged(); NotifyPropertyListChanged();
    }
    private void EnsureFileBudget(int replacedTrack, int keyCount)
    {
        var fields = 16L + 8L * _tracks.Count;
        for (var i = 0; i < _tracks.Count; i++) fields += (_tracks[i].Kind == TrackType.Method ? 2L : _tracks[i].Kind == TrackType.Audio ? 3L : 1L) * (i == replacedTrack ? keyCount : _tracks[i].Times.Count);
        if (_tracks.Count > 4096 || fields > 65536) throw new InvalidDataException("Animation fields exceed archive budget.");
    }
    private void SetFileTimes(int track, double[] times)
    {
        ArgumentNullException.ThrowIfNull(times); EnsureFileBudget(track, times.Length);
        for (var i = 0; i < times.Length; i++)
            if (!double.IsFinite(times[i]) || (i > 0 && times[i] <= times[i - 1])) throw new InvalidDataException("Animation key times must be finite and strictly increasing.");
        Get(track).SetFileTimes(times); EmitChanged(); NotifyPropertyListChanged();
    }
    private void SetFileTransitions(int track, double[] transitions)
    {
        ArgumentNullException.ThrowIfNull(transitions); var item = Get(track);
        if (transitions.Length != item.Times.Count || transitions.Any(v => !double.IsFinite(v))) throw new InvalidDataException("Animation transitions do not match the keys.");
        item.Transitions.Clear(); item.Transitions.AddRange(transitions); EmitChanged();
    }
    private byte[] GetFileMarkers()
    {
        if (_markers.Count > 65536) throw new InvalidDataException("Animation markers exceed file budget.");
        using var stream = new StreamPeerBuffer(); stream.Put32(1); stream.Put32(_markers.Count);
        foreach (var name in GetMarkerNames())
        {
            var marker = _markers[name]; ResourceArchiveStrings.Write(stream, name); stream.PutDouble(marker.Time);
            stream.PutFloat(marker.Color.R); stream.PutFloat(marker.Color.G); stream.PutFloat(marker.Color.B); stream.PutFloat(marker.Color.A);
        }
        return stream.DataArray;
    }
    private void SetFileMarkers(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length < 8 || bytes.Length > ResourceArchive.Limit) throw new InvalidDataException("Invalid animation marker payload size.");
        using var stream = new StreamPeerBuffer { DataArray = bytes };
        if (stream.Get32() != 1) throw new InvalidDataException("Unsupported animation marker schema.");
        var count = stream.Get32();
        if (count < 0 || count > 65536 || count > stream.GetAvailableBytes() / 28) throw new InvalidDataException("Animation marker count exceeds file budget.");
        var markers = new Dictionary<string, (double Time, Color Color)>(StringComparer.Ordinal);
        var previous = double.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            var name = ResourceArchiveStrings.Read(stream); var time = stream.GetDouble();
            var color = new Color(stream.GetFloat(), stream.GetFloat(), stream.GetFloat(), stream.GetFloat());
            if (string.IsNullOrEmpty(name) || !double.IsFinite(time) || time - previous <= 1e-5 ||
                !float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) || !float.IsFinite(color.A) || !markers.TryAdd(name, (time, color)))
                throw new InvalidDataException("Invalid animation marker.");
            previous = time;
        }
        if (stream.GetAvailableBytes() != 0) throw new InvalidDataException("Trailing animation marker bytes.");
        _markers.Clear(); foreach (var item in markers) _markers.Add(item.Key, item.Value); EmitChanged();
    }
}

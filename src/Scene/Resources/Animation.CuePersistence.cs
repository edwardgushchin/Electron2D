namespace Electron2D;

internal sealed partial class AnimationAudioTrack
{
    internal override IEnumerable<PropertyDescriptor> FileMetadataProperties(int track)
    {
        yield return new PropertyDescriptor<Animation, bool>($"Track[{track}].UseBlend", a => a.AudioTrackIsUseBlend(track), (a, v) => a.AudioTrackSetUseBlend(track, v), stored: true);
    }
    internal override IEnumerable<PropertyDescriptor> FileKeyProperties(int track)
    {
        for (var key = 0; key < Times.Count; key++)
        {
            var index = key; var prefix = $"Track[{track}].Key[{key}].";
            yield return new PropertyDescriptor<Animation, AudioStream?>(prefix + "Stream", a => a.AudioTrackGetKeyStream(track, index), (a, v) => a.AudioTrackSetKeyStream(track, index, v), stored: true);
            yield return new PropertyDescriptor<Animation, double>(prefix + "StartOffset", a => a.AudioTrackGetKeyStartOffset(track, index), (a, v) => a.AudioTrackSetKeyStartOffset(track, index, v), stored: true);
            yield return new PropertyDescriptor<Animation, double>(prefix + "EndOffset", a => a.AudioTrackGetKeyEndOffset(track, index), (a, v) => a.AudioTrackSetKeyEndOffset(track, index, v), stored: true);
        }
    }
}
internal sealed partial class AnimationMethodTrack<TOwner> where TOwner : Node
{
    internal override string FileReceiverID => ResourceFileTypes.AnimationMethodReceiverID<TOwner>();
    internal override IEnumerable<PropertyDescriptor> FileMetadataProperties(int track)
    {
        yield return new PropertyDescriptor<Animation, string[]>($"Track[{track}].MethodSchemas", a => ((AnimationMethodTrack<TOwner>)a.Get(track)).GetFileMethodSchemas(),
            (a, v) => ((AnimationMethodTrack<TOwner>)a.Get(track)).SetFileMethodSchemas(a, v), stored: true);
    }
    private string[] GetFileMethodSchemas()
    {
        var ids = new string[Values.Count];
        for (var i = 0; i < ids.Length; i++) ids[i] = ResourceFileTypes.AnimationMethodBindingID(Values[i]);
        return ids;
    }
    private void SetFileMethodSchemas(Animation animation, string[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Length != Times.Count) throw new InvalidDataException("Animation method schemas do not match the keys.");
        var keys = new AnimationMethodKey<TOwner>[ids.Length];
        for (var i = 0; i < keys.Length; i++) keys[i] = ResourceFileTypes.AnimationMethodBinding(ids[i]).CreateKey<TOwner>();
        Values.Clear(); Values.AddRange(keys); animation.NotifyFileSchemaChanged();
    }
    internal override IEnumerable<PropertyDescriptor> FileKeyProperties(int track)
    {
        for (var key = 0; key < Values.Count; key++)
            if (Values[key] is { } value)
                foreach (var descriptor in value.FileProperties(track, key)) yield return descriptor;
    }
}

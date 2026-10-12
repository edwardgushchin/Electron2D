namespace Electron2D;

public sealed partial class AudioStreamWAV
{
    private static readonly PropertyDescriptor[] WAVFileProperties =
    [
        new PropertyDescriptor<AudioStreamWAV, Format>(nameof(SampleFormat), a => a.SampleFormat, (a, v) => a.SampleFormat = v, _ => Format.PCM8, stored: true),
        new PropertyDescriptor<AudioStreamWAV, int>(nameof(MixRate), a => a.MixRate, (a, v) => a.MixRate = v, _ => 44100, stored: true),
        new PropertyDescriptor<AudioStreamWAV, bool>(nameof(Stereo), a => a.Stereo, (a, v) => a.Stereo = v, _ => false, stored: true),
        new PropertyDescriptor<AudioStreamWAV, AudioLoopMode>(nameof(Loop), a => a.Loop, (a, v) => a.Loop = v, _ => AudioLoopMode.Disabled, stored: true),
        new PropertyDescriptor<AudioStreamWAV, int>(nameof(LoopBegin), a => a.LoopBegin, (a, v) => a.LoopBegin = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioStreamWAV, int>(nameof(LoopEnd), a => a.LoopEnd, (a, v) => a.LoopEnd = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioStreamWAV, byte[]>(nameof(Data), a => a.Data, (a, v) => a.Data = v, _ => [], stored: true),
        new PropertyDescriptor<AudioStreamWAV, Dictionary<string, string>>(nameof(Tags), a => a.Tags, (a, v) => a.Tags = v, _ => new(StringComparer.Ordinal), stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(WAVFileProperties);
}

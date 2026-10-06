namespace Electron2D;

public static partial class ResourceFileTypes
{
    private static void RegisterAudioFileResources()
    {
        RegisterResource("AudioBusLayout", CreateAudioBusLayoutFileResource);
        RegisterResource("AudioEffectAmplify", CreateAudioEffectAmplifyFileResource);
        RegisterResource("AudioEffectBandLimitFilter", CreateAudioEffectBandLimitFilterFileResource);
        RegisterResource("AudioEffectBandPassFilter", CreateAudioEffectBandPassFilterFileResource);
        RegisterResource("AudioEffectCapture", CreateAudioEffectCaptureFileResource);
        RegisterResource("AudioEffectChorus", CreateAudioEffectChorusFileResource);
        RegisterResource("AudioEffectCompressor", CreateAudioEffectCompressorFileResource);
        RegisterResource("AudioEffectDelay", CreateAudioEffectDelayFileResource);
        RegisterResource("AudioEffectDistortion", CreateAudioEffectDistortionFileResource);
        RegisterResource("AudioEffectEQ", CreateAudioEffectEQFileResource);
        RegisterResource("AudioEffectEQ10", CreateAudioEffectEQ10FileResource);
        RegisterResource("AudioEffectEQ21", CreateAudioEffectEQ21FileResource);
        RegisterResource("AudioEffectEQ6", CreateAudioEffectEQ6FileResource);
        RegisterResource("AudioEffectFilter", CreateAudioEffectFilterFileResource);
        RegisterResource("AudioEffectHardLimiter", CreateAudioEffectHardLimiterFileResource);
        RegisterResource("AudioEffectHighPassFilter", CreateAudioEffectHighPassFilterFileResource);
        RegisterResource("AudioEffectHighShelfFilter", CreateAudioEffectHighShelfFilterFileResource);
        RegisterResource("AudioEffectLimiter", CreateAudioEffectLimiterFileResource);
        RegisterResource("AudioEffectLowPassFilter", CreateAudioEffectLowPassFilterFileResource);
        RegisterResource("AudioEffectLowShelfFilter", CreateAudioEffectLowShelfFilterFileResource);
        RegisterResource("AudioEffectNotchFilter", CreateAudioEffectNotchFilterFileResource);
        RegisterResource("AudioEffectPanner", CreateAudioEffectPannerFileResource);
        RegisterResource("AudioEffectPhaser", CreateAudioEffectPhaserFileResource);
        RegisterResource("AudioEffectPitchShift", CreateAudioEffectPitchShiftFileResource);
        RegisterResource("AudioEffectRecord", CreateAudioEffectRecordFileResource);
        RegisterResource("AudioEffectReverb", CreateAudioEffectReverbFileResource);
        RegisterResource("AudioEffectSpectrumAnalyzer", CreateAudioEffectSpectrumAnalyzerFileResource);
        RegisterResource("AudioEffectStereoEnhance", CreateAudioEffectStereoEnhanceFileResource);
    }
    private static AudioBusLayout CreateAudioBusLayoutFileResource() => new();
    private static AudioEffectAmplify CreateAudioEffectAmplifyFileResource() => new();
    private static AudioEffectBandLimitFilter CreateAudioEffectBandLimitFilterFileResource() => new();
    private static AudioEffectBandPassFilter CreateAudioEffectBandPassFilterFileResource() => new();
    private static AudioEffectCapture CreateAudioEffectCaptureFileResource() => new();
    private static AudioEffectChorus CreateAudioEffectChorusFileResource() => new();
    private static AudioEffectCompressor CreateAudioEffectCompressorFileResource() => new();
    private static AudioEffectDelay CreateAudioEffectDelayFileResource() => new();
    private static AudioEffectDistortion CreateAudioEffectDistortionFileResource() => new();
    private static AudioEffectEQ CreateAudioEffectEQFileResource() => new();
    private static AudioEffectEQ10 CreateAudioEffectEQ10FileResource() => new();
    private static AudioEffectEQ21 CreateAudioEffectEQ21FileResource() => new();
    private static AudioEffectEQ6 CreateAudioEffectEQ6FileResource() => new();
    private static AudioEffectFilter CreateAudioEffectFilterFileResource() => new();
    private static AudioEffectHardLimiter CreateAudioEffectHardLimiterFileResource() => new();
    private static AudioEffectHighPassFilter CreateAudioEffectHighPassFilterFileResource() => new();
    private static AudioEffectHighShelfFilter CreateAudioEffectHighShelfFilterFileResource() => new();
    private static AudioEffectLimiter CreateAudioEffectLimiterFileResource() => new();
    private static AudioEffectLowPassFilter CreateAudioEffectLowPassFilterFileResource() => new();
    private static AudioEffectLowShelfFilter CreateAudioEffectLowShelfFilterFileResource() => new();
    private static AudioEffectNotchFilter CreateAudioEffectNotchFilterFileResource() => new();
    private static AudioEffectPanner CreateAudioEffectPannerFileResource() => new();
    private static AudioEffectPhaser CreateAudioEffectPhaserFileResource() => new();
    private static AudioEffectPitchShift CreateAudioEffectPitchShiftFileResource() => new();
    private static AudioEffectRecord CreateAudioEffectRecordFileResource() => new();
    private static AudioEffectReverb CreateAudioEffectReverbFileResource() => new();
    private static AudioEffectSpectrumAnalyzer CreateAudioEffectSpectrumAnalyzerFileResource() => new();
    private static AudioEffectStereoEnhance CreateAudioEffectStereoEnhanceFileResource() => new();
}

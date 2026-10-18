using System.Runtime.InteropServices;

namespace Electron2D;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleStateLayout
{
    internal Vector4 TransformX;
    internal Vector4 TransformY;
    internal Vector4 Color;
    internal Vector4 Custom;
    internal Vector4 VelocityAge;
    internal Vector4i State;
    internal const int ByteSize = 96;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleFrameLayout
{
    internal Vector4i Counts;
    internal Vector4 Time;
    internal Vector4 Cycle;
    internal Vector4 EmissionX;
    internal Vector4 EmissionY;
    internal Vector4 PreviousEmissionX;
    internal Vector4 PreviousEmissionY;
    internal Vector4 EmitterVelocity;
    internal Vector4 SDFRect;
    internal Vector4 SDFTransformX;
    internal Vector4 SDFTransformY;
    internal Vector4i Options;
    internal const int ByteSize = 192;
}

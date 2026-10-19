#version 450
#extension GL_GOOGLE_include_directive : require
#include "Includes/electron2d/particles.glslinc"
layout(local_size_x = 64) in;
layout(std430, set = 1, binding = 1) buffer Intake { Electron2D_Particle EMISSIONS[]; };
uint particleHash(uint value)
{
    value = ((value >> 16) ^ value) * 73244475u;
    value = ((value >> 16) ^ value) * 73244475u;
    return (value >> 16) ^ value;
}
void birth(inout Electron2D_Particle particle, uint index)
{
    particle.TransformX = (Options.x & 4u) != 0 ? vec4(1, 0, 0, 0) : EmissionX;
    particle.TransformY = (Options.x & 4u) != 0 ? vec4(0, 1, 0, 0) : EmissionY;
    particle.Color = vec4(1); particle.Custom = vec4(0); particle.VelocityAge = vec4(0);
    particle.State = uvec4(3, Counts.y, particleHash(Counts.z + index), floatBitsToUint(Time.x));
}
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * Options.w;
    if (index >= Counts.x) return;
    Electron2D_Particle particle = PARTICLES[index];
    if ((Options.x & 8u) != 0)
    {
        particle.TransformX = vec4(1, 0, 0, 0); particle.TransformY = vec4(0, 1, 0, 0);
        particle.Color = vec4(1); particle.Custom = vec4(0); particle.VelocityAge = vec4(0); particle.State = uvec4(0);
    }
    particle.State.x &= ~(2u | (31u << 8u));
    float localDelta = Time.x;
    if ((particle.State.x & 1u) != 0)
    {
        particle.TransformX.w += particle.VelocityAge.x * Time.x;
        particle.TransformY.w += particle.VelocityAge.y * Time.x;
        particle.VelocityAge.z += Time.x;
    }
    float phase = float(index) / float(Counts.x);
    uint cycle = Counts.y - (phase >= Cycle.y ? 1u : 0u);
    float jitter = float(particleHash((cycle << 16u) + index) % 65536u) / 65536.0;
    phase += Time.z * jitter / float(Counts.x);
    phase *= 1.0 - Time.w;
    bool crossed = Cycle.y >= Cycle.x ? phase >= Cycle.x && phase < Cycle.y : phase >= Cycle.x || phase < Cycle.y;
    crossed = Time.x > 0.0 && (crossed || (Options.x & 16u) != 0);
    if (crossed)
    {
        particle.State.x = 0;
        if ((Options.x & 1u) != 0 && float(index) < float(Counts.x) * Cycle.w)
        {
            birth(particle, index);
            if ((Options.x & 2u) != 0) localDelta = (Cycle.y >= phase ? Cycle.y - phase : 1.0 + Cycle.y - phase) * Time.y;
        }
    }
    if ((particle.State.x & 1u) == 0 && EMISSIONS.length() > 0)
    {
        uint expected = atomicAdd(EMISSIONS[0].State.x, 0);
        while (expected != 0)
        {
            uint observed = atomicCompSwap(EMISSIONS[0].State.x, expected, expected - 1u);
            if (observed == expected)
            {
                uint slot = min(expected, uint(EMISSIONS.length() - 1));
                Electron2D_Particle incoming = EMISSIONS[slot]; uint flags = incoming.State.x;
                birth(particle, index);
                particle.State.x |= (flags & 31u) << 8u;
                if ((flags & 1u) != 0) { particle.TransformX.w = incoming.TransformX.w; particle.TransformY.w = incoming.TransformY.w; }
                if ((flags & 2u) != 0) { particle.TransformX.xy = incoming.TransformX.xy; particle.TransformY.xy = incoming.TransformY.xy; }
                if ((flags & 4u) != 0) particle.VelocityAge.xy = incoming.VelocityAge.xy;
                if ((flags & 8u) != 0) particle.Color = incoming.Color;
                if ((flags & 16u) != 0) particle.Custom = incoming.Custom;
                break;
            }
            expected = observed;
        }
    }
    particle.State.w = floatBitsToUint(localDelta);
    if ((Options.x & 32u) != 0 && particle.VelocityAge.z >= Time.y) particle.State.x &= ~1u;
    PARTICLES[index] = particle;
}

#version 450
#extension GL_GOOGLE_include_directive : require
#define ELECTRON2D_PARTICLE_SUBEMISSION
#include "electron2d/particles.glslinc"
layout(local_size_x = 64) in;
layout(std140, set = 2, binding = 1) uniform Parameters { vec4 Tint; vec2 Velocity; uint Operation; float InputValue; };
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * Options.w;
    if (index >= Counts.x || (PARTICLES[index].State.x & 1u) == 0) return;
    Electron2D_Particle particle = PARTICLES[index];
    if ((particle.State.x & 2u) != 0)
    {
        particle.VelocityAge.xy = Velocity;
        if ((Operation & 1u) != 0) { particle.TransformX = vec4(1, 0, 0, 8 + index * 6); particle.TransformY = vec4(0, 1, 0, 20); }
    }
    if ((Operation & 32u) == 0) particle.Color = Tint;
    else if ((particle.State.x & 2u) != 0) particle.Color = (Counts.y & 1u) == 0 ? vec4(1, 0, 0, 1) : vec4(0, 0, 1, 1);
    if ((Operation & 2u) != 0 && particle.VelocityAge.z >= Time.y) particle.State.x = 0;
    if ((Operation & 4u) != 0 && (particle.State.x & 2u) != 0)
    {
        Electron2D_Particle child = particle; child.State.x = 31u; Electron2D_EmitSubparticle(child);
    }
    if ((Operation & 8u) != 0) particle.Color = vec4(Cycle.z, EmitterVelocity.x, EmitterVelocity.w, 1);
    if ((Operation & 16u) != 0) particle.Color = vec4(uintBitsToFloat(particle.State.w), particle.VelocityAge.z, 0, 1);
    if ((Operation & 64u) != 0) particle.Color = vec4(EmissionX.w, PreviousEmissionX.w, (Options.x & 4u) != 0 ? 1 : 0, 1);
    if ((Operation & 128u) != 0) { particle.Color = vec4(particle.VelocityAge.z, 0, 1 - particle.VelocityAge.z, 1); particle.Custom = particle.Color; }
    if ((Operation & 256u) != 0) { uint flags = (particle.State.x & ELECTRON2D_PARTICLE_EMIT_FLAGS_MASK) >> ELECTRON2D_PARTICLE_EMIT_FLAGS_SHIFT; particle.Color = vec4((flags & 1u) != 0 ? 1 : 0, (flags & 4u) != 0 ? 1 : 0, (flags & 8u) != 0 ? 1 : 0, 1); }
    PARTICLES[index] = particle;
}

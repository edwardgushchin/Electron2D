#version 450
#extension GL_GOOGLE_include_directive : require
#define ELECTRON2D_PARTICLE_SUBEMISSION
#include "electron2d/particles.glslinc"
layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;
layout(std140, set = 2, binding = 1) uniform Parameters { vec4 Tint; vec2 Drift; bool Enabled; };
void main()
{
    uint index = gl_GlobalInvocationID.x;
    if (index >= Counts.x) return;
    Electron2D_Particle particle = PARTICLES[index];
    if (Enabled)
    {
        particle.TransformX.w += Drift.x * Time.x;
        particle.TransformY.w += Drift.y * Time.x;
        particle.Color = Tint;
        particle.Custom.x = float(index);
        particle.VelocityAge.z += Time.x;
        particle.State.x = 1;
        Electron2D_EmitSubparticle(particle);
    }
    PARTICLES[index] = particle;
}

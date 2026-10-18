#define ELECTRON2D_PARTICLE_SUBEMISSION
#include "electron2d/particles.hlslinc"
cbuffer Parameters : register(b1, space2) { float4 Tint; float2 Drift; bool Enabled; };
[numthreads(64, 1, 1)]
void main(uint3 thread : SV_DispatchThreadID)
{
    uint index = thread.x;
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

#include "electron2d/particles.hlslinc"
cbuffer Values:register(b1,space2) { float4 tint:packoffset(c0) = float4(.25,.5,.75,1); bool enabled:packoffset(c1) = true; };
[numthreads(64,1,1)]
void main(uint3 thread:SV_DispatchThreadID) {
    if (thread.x >= Counts.x) return;
    Electron2D_Particle particle=PARTICLES[thread.x];
    if (particle.State.x != 0) particle.Color=enabled ? tint : float4(1,0,1,1);
    PARTICLES[thread.x]=particle;
}

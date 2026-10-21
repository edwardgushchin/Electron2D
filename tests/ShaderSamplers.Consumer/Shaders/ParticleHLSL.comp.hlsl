#include "electron2d/particles.hlslinc"
Texture2D<float4> pixels : register(t0,space0);
SamplerState pixelSampler : register(s0,space0);
[numthreads(64,1,1)]
void main(uint3 thread : SV_DispatchThreadID) {
 uint index = thread.x; if(index >= Counts.x) return;
 Electron2D_Particle particle = PARTICLES[index]; particle.Color = pixels.SampleLevel(pixelSampler,float2(.5,.25),0);
 particle.State.x = 1; PARTICLES[index] = particle;
}

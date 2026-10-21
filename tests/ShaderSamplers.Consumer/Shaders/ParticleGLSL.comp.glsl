#version 450
#extension GL_GOOGLE_include_directive : require
#include "electron2d/particles.glslinc"
layout(local_size_x=64,local_size_y=1,local_size_z=1) in;
layout(set=0,binding=0) uniform sampler2D pixels;
void main() {
 uint index = gl_GlobalInvocationID.x; if(index >= Counts.x) return;
 Electron2D_Particle particle = PARTICLES[index]; particle.Color = textureLod(pixels,vec2(.5,.25),0);
 particle.State.x = 1; PARTICLES[index] = particle;
}

#version 450
#extension GL_GOOGLE_include_directive : require
#include "electron2d/particles.glslinc"
layout(set=2,binding=1,std140) uniform Values { vec4 tint; bool enabled; };
layout(local_size_x=64,local_size_y=1,local_size_z=1) in;
void main() {
    uint index=gl_GlobalInvocationID.x;
    if (index >= Counts.x) return;
    Electron2D_Particle particle=PARTICLES[index];
    if (particle.State.x != 0) particle.Color=enabled ? tint : vec4(1,0,1,1);
    PARTICLES[index]=particle;
}

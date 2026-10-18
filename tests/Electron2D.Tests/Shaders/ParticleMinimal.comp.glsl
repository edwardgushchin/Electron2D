#version 450
#extension GL_GOOGLE_include_directive : require
#include "electron2d/particles.glslinc"
layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;
void main()
{
    uint index = gl_GlobalInvocationID.x;
    if (index < Counts.x) PARTICLES[index].State.x = Counts.y;
}

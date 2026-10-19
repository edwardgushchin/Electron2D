#version 450
#extension GL_GOOGLE_include_directive : require
#include "electron2d/particles.glslinc"
layout(local_size_x = 64) in;
layout(set = 2, binding = 1, std140) uniform Globals { vec4 rollbackProcessTint; };
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * Options.w;
    if (index < Counts.x && (PARTICLES[index].State.x & 1u) != 0) PARTICLES[index].Color = rollbackProcessTint;
}

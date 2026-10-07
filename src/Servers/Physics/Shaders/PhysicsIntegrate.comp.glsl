#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsBody.inc.glsl"
layout(local_size_x = 64) in;
layout(std430, set = 1, binding = 0) buffer Bodies { Body bodies[]; };
layout(std140, set = 2, binding = 0) uniform Step { vec4 step; vec4 control; };
void main()
{
    uint i = gl_GlobalInvocationID.x;
    if (i >= uint(control.z)) return;
    Body b = bodies[i];
    integrateBody(b, step, control);
    bodies[i] = b;
}

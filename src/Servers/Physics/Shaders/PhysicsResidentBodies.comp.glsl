#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
layout(local_size_x = 64) in;

struct Command { uvec4 header; ResidentBody body; vec4 impulse; };
struct Snapshot { vec4 pose; vec4 velocity; };
layout(std430, set = 0, binding = 0) readonly buffer Commands { Command commands[]; };
layout(std430, set = 0, binding = 1) readonly buffer Requests { uvec4 requests[]; };
layout(std430, set = 1, binding = 0) buffer Bodies { ResidentBody bodies[]; };
layout(std430, set = 1, binding = 1) buffer Status { uint status; };
layout(std430, set = 1, binding = 2) buffer Results { Snapshot results[]; };
layout(std140, set = 2, binding = 0) uniform Settings { vec4 step; uvec4 control; };

bool finite4(vec4 v) { return !any(isnan(v)) && !any(isinf(v)); }
void fail(uint value) { atomicOr(status, value); }

void main()
{
    uint i = gl_GlobalInvocationID.x;
    if (i >= control.y) return;
    if (control.x == 0u)
    {
        Command c = commands[i];
        uint index = c.header.x, generation = c.header.y, mask = c.header.z;
        if (index >= control.z) { fail(1u); return; }
        if ((mask & 2u) != 0u) { bodies[index] = ResidentBody(vec4(0), vec4(0), vec4(0), vec4(0), uvec4(generation, 0, 0, 0)); return; }
        ResidentBody b;
        if ((mask & 1u) != 0u) b = c.body;
        else
        {
            b = bodies[index];
            if (b.flags.x != generation || b.flags.w == 0u) { fail(1u); return; }
        }
        if ((mask & 4u) != 0u) b.pose = c.body.pose;
        if ((mask & 8u) != 0u) b.velocity = c.body.velocity;
        if ((mask & 32u) != 0u) b.force.xyz = c.body.force.xyz;
        if ((mask & 16u) != 0u && b.flags.y >= 2u)
        {
            b.velocity.xy += c.impulse.xy * b.properties.x;
            if ((b.flags.z & 4u) == 0u) b.velocity.z += c.impulse.z * b.properties.y;
        }
        if ((b.flags.z & 4u) != 0u) b.velocity.z = 0;
        if (!finite4(b.pose) || !finite4(b.velocity)) { fail(2u); return; }
        bodies[index] = b;
    }
    else if (control.x == 1u)
    {
        ResidentBody b = bodies[i];
        if (b.flags.w == 0u || b.flags.y == 0u) return;
        float dt = step.z;
        if (b.flags.y >= 2u)
        {
            b.velocity.xy *= max(0.0, 1.0 - dt * b.properties.z);
            b.velocity.z *= max(0.0, 1.0 - dt * b.properties.w);
            b.velocity.xy += dt * (step.xy * b.force.w + b.force.xy * b.properties.x);
            b.velocity.z += dt * b.force.z * b.properties.y;
        }
        if ((b.flags.z & 4u) != 0u) b.velocity.z = 0;
        b.pose.xy += dt * b.velocity.xy;
        float angle = dt * b.velocity.z;
        vec2 q = vec2(cos(angle), sin(angle));
        b.pose.zw = vec2(b.pose.z * q.x - b.pose.w * q.y, b.pose.w * q.x + b.pose.z * q.y);
        b.pose.zw *= inversesqrt(dot(b.pose.zw, b.pose.zw));
        if (!finite4(b.pose) || !finite4(b.velocity)) { fail(2u); return; }
        bodies[i] = b;
    }
    else
    {
        uvec4 request = requests[i];
        if (request.x >= control.z) { fail(1u); return; }
        ResidentBody b = bodies[request.x];
        if (b.flags.w == 0u || b.flags.x != request.y) { fail(1u); return; }
        results[i] = Snapshot(b.pose, b.velocity);
    }
}

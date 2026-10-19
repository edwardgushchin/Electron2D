#version 450
layout(local_size_x = 64) in;
struct Particle { vec4 x; vec4 y; vec4 color; vec4 custom; vec4 velocityAge; uvec4 state; };
layout(std430, set = 0, binding = 0) readonly buffer State { Particle particles[]; };
layout(std430, set = 1, binding = 0) buffer Ordering { uvec2 ordering[]; };
layout(std140, set = 2, binding = 0) uniform Parameters { uvec4 counts; uvec4 extent; };
bool lessValue(uvec2 a, uvec2 b) { return a.x < b.x || (a.x == b.x && a.y < b.y); }
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * extent.x; if (index >= counts.y) return;
    if (counts.z == 0)
    {
        uint key = 0xffffffffu;
        if (index < counts.x)
        {
            float age = particles[index].velocityAge.z;
            key = counts.w == 0 ? index : floatBitsToUint(max(0, age));
            if (counts.w == 1) key = 0xfffffffeu - min(key, 0xfffffffeu);
            if ((particles[index].state.x & 1u) == 0 || isnan(age) || isinf(age)) key = 0xffffffffu;
        }
        ordering[index] = uvec2(key, index); return;
    }
    uint other = index ^ counts.z; if (other <= index || other >= counts.y) return;
    uvec2 a = ordering[index], b = ordering[other]; bool ascending = (index & counts.w) == 0;
    if (lessValue(b, a) == ascending) { ordering[index] = b; ordering[other] = a; }
}

#version 450
layout(local_size_x = 64) in;
struct Particle { vec4 x; vec4 y; vec4 color; vec4 custom; vec4 velocityAge; uvec4 state; };
layout(std430, set = 0, binding = 0) readonly buffer Source { Particle source[]; };
layout(std430, set = 1, binding = 0) buffer Target { Particle target[]; };
layout(std140, set = 2, binding = 0) uniform Parameters { vec4 transformX; vec4 transformY; uvec4 counts; };
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * counts.z;
    if (index >= min(source[0].state.x, uint(source.length() - 1))) return;
    Particle particle = source[index + 1];
    particle.x = vec4(transformX.x * particle.x.xy + transformX.y * particle.y.xy, 0, dot(transformX.xy, vec2(particle.x.w, particle.y.w)) + transformX.w);
    particle.y = vec4(transformY.x * source[index + 1].x.xy + transformY.y * source[index + 1].y.xy, 0, dot(transformY.xy, vec2(source[index + 1].x.w, source[index + 1].y.w)) + transformY.w);
    particle.velocityAge.xy = vec2(dot(transformX.xy, particle.velocityAge.xy), dot(transformY.xy, particle.velocityAge.xy));
    uint expected = atomicAdd(target[0].state.x, 0); uint capacity = min(target[0].state.y, uint(target.length() - 1));
    for (;;)
    {
        if (expected >= capacity) { atomicOr(target[0].state.z, 1); return; }
        uint observed = atomicCompSwap(target[0].state.x, expected, expected + 1);
        if (observed == expected) { target[expected + 1] = particle; return; }
        expected = observed;
    }
}

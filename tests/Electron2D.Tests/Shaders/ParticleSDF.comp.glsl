#version 450
#extension GL_GOOGLE_include_directive : require
#include "electron2d/particles.glslinc"
layout(local_size_x = 64) in;
layout(set = 0, binding = 0) uniform sampler2D SDF_TEXTURE;
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * Options.w;
    if (index >= Counts.x || (PARTICLES[index].State.x & 1u) == 0) return;
    vec2 position = vec2(PARTICLES[index].TransformX.w, PARTICLES[index].TransformY.w);
    vec2 uv = vec2(dot(SDFTransformX.xy, position) + SDFTransformX.w, dot(SDFTransformY.xy, position) + SDFTransformY.w);
    float distance = textureLod(SDF_TEXTURE, uv, 0).r;
    PARTICLES[index].Color = distance < 0 ? vec4(1, 0, 0, 1) : vec4(0, 0, 1, 1);
}

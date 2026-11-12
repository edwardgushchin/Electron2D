#version 450
#extension GL_GOOGLE_include_directive : require
#include "Includes/electron2d/smaa.glslinc"

layout(location = 1) in vec2 uv;

layout(set = 2, binding = 0) uniform sampler2D edgeImage;
layout(set = 2, binding = 1) uniform sampler2D areaLookup;
layout(set = 2, binding = 2) uniform sampler2D searchLookup;
layout(location = 0) out vec4 outputWeights;

void main()
{
    vec4 unusedPosition;
    vec2 pixel;
    vec4 neighbors[3];
    vec2 centerUV = uv;
    SMAABlendingWeightCalculationVS(vec4(0.0), unusedPosition, centerUV, pixel, neighbors);
    outputWeights = SMAABlendingWeightCalculationPS(centerUV, pixel, neighbors, edgeImage, areaLookup, searchLookup, ivec4(0));
}

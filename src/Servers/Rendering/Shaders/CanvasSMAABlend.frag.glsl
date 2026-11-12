#version 450
#extension GL_GOOGLE_include_directive : require
#include "Includes/electron2d/smaa.glslinc"

layout(location = 1) in vec2 uv;

layout(set = 2, binding = 0) uniform sampler2D colorImage;
layout(set = 2, binding = 1) uniform sampler2D weightImage;
layout(location = 0) out vec4 outputColor;

void main()
{
    vec4 unusedPosition;
    vec4 neighbors[2];
    vec2 centerUV = uv;
    SMAANeighborhoodBlendingVS(vec4(0.0), unusedPosition, centerUV, neighbors);
    outputColor = SMAANeighborhoodBlendingPS(centerUV, neighbors, colorImage, weightImage);
}

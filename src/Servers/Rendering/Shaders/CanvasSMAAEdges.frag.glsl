#version 450
#extension GL_GOOGLE_include_directive : require
#include "Includes/electron2d/smaa.glslinc"

layout(location = 1) in vec2 uv;

layout(set = 2, binding = 0) uniform sampler2D colorImage;
layout(location = 0) out vec2 outputEdges;

vec4 signalAt(vec2 at)
{
    vec4 pixel = textureLod(colorImage, at, 0.0);
    if (metrics.z != 0.0) pixel.rgb = sqrt(max(pixel.rgb, vec3(0.0)));
    return vec4(pixel.rgb, clamp(pixel.a, 0.0, 1.0));
}

float contrast(vec4 center, vec2 at)
{
    vec4 difference = abs(center - signalAt(at));
    return max(max(difference.r, difference.g), max(difference.b, difference.a));
}

void main()
{
    vec4 unusedPosition;
    vec4 neighbors[3];
    vec2 centerUV = uv;
    SMAAEdgeDetectionVS(vec4(0.0), unusedPosition, centerUV, neighbors);
    vec4 center = signalAt(centerUV);
    vec2 deltas = vec2(contrast(center, neighbors[0].xy), contrast(center, neighbors[0].zw));
    vec2 edges = step(vec2(SMAA_THRESHOLD), deltas);
    if (edges.x + edges.y == 0.0)
    {
        outputEdges = vec2(0.0);
        return;
    }
    float localMaximum = max(deltas.x, deltas.y);
    for (int i = 1; i <= 2; ++i)
        localMaximum = max(localMaximum, max(contrast(center, neighbors[i].xy), contrast(center, neighbors[i].zw)));
    outputEdges = edges * step(vec2(0.5 * localMaximum), deltas);
}

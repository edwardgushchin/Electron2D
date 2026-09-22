#version 450
layout(location = 0) in vec4 color;
layout(location = 0) out vec4 outputColor;
layout(set = 2, binding = 1) uniform sampler2D colorMap;
layout(set = 2, binding = 0) uniform sampler2D detailMap;
layout(set = 3, binding = 0, std140) uniform Material { vec4 tint; float lod; float detailAmount; };
void main() {
    vec2 uv = gl_FragCoord.xy / 64;
    outputColor = color * tint * textureLod(colorMap, uv, lod) + detailAmount * textureLod(detailMap, uv, lod);
}

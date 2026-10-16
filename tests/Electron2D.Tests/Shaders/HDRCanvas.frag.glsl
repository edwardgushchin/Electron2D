#version 450
layout(location = 0) in vec4 color;
layout(location = 1) in vec2 uv;
layout(location = 0) out vec4 outputColor;
layout(set = 2, binding = 0) uniform sampler2D TEXTURE;
layout(set = 2, binding = 1) uniform sampler2D values;
layout(set = 3, binding = 0, std140) uniform Parameters { float lod; float multiplier; float dataWeight; };
void main() {
    outputColor = color * multiplier * textureLod(TEXTURE, uv, lod) + dataWeight * textureLod(values, uv, 0);
    outputColor.a = 1;
}

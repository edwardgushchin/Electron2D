#version 450
layout(location = 0) in vec4 color;
layout(location = 1) in vec2 uv;
layout(set = 2, binding = 0) uniform sampler2D TEXTURE;
layout(location = 0) out vec4 outputColor;
void main() { outputColor = color * texture(TEXTURE, uv); }

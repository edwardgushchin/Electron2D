#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform usampler2D TEXTURE;
void main() { result = color * vec4(texture(TEXTURE, uv)); }

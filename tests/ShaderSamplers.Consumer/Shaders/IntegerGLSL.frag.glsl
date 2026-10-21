#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform usampler2D pixels;
layout(set=3,binding=0,std140) uniform Parameters { vec4 Coordinates; };
void main() { result = vec4(float(texture(pixels, uv * Coordinates.zw + Coordinates.xy).r) / 32.0, 0, 0, 1) * color; }

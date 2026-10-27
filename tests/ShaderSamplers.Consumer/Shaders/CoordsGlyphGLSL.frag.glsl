#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform sampler2D TEXTURE;
layout(set=3,binding=0,std140) uniform Parameters { vec4 Coordinates; };
void main() { result = textureLod(TEXTURE, uv * Coordinates.zw + Coordinates.xy, 0) * color; }

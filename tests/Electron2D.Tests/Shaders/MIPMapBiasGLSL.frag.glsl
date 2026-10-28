#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 outputColor;
layout(set=2,binding=0) uniform sampler2D colorMaps[2];
layout(set=3,binding=0,std140) uniform Material { int selected; };
void main() { outputColor=color*texture(colorMaps[selected],uv); }

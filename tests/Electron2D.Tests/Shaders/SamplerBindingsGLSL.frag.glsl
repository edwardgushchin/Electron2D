#version 450
layout(location=0) in vec4 color;
layout(location=0) out vec4 outputColor;
layout(set=2,binding=0) uniform sampler2D colorMaps[2];
layout(set=3,binding=0,std140) uniform Material { int selected; };
vec4 readMap(sampler2D source, vec2 uv) { return texture(source,uv); }
void main() { outputColor=color*readMap(colorMaps[selected],vec2(0.5)); }

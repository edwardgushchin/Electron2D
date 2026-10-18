#version 450
layout(location=0) in vec4 color;
layout(location=0) out vec4 outputColor;
layout(set=3,binding=0,std140) uniform Material { vec4 tint; };
void main() { outputColor=color*tint; }

#version 450
layout(set=3,binding=0,std140) uniform Instance { float values[2]; };
layout(location=0) out vec4 outputColor;
void main() { outputColor = vec4(values[0]+values[1]); }

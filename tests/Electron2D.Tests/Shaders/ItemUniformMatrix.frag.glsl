#version 450
layout(set=3,binding=0,std140) uniform Instance { mat2 basis; };
layout(location=0) out vec4 outputColor;
void main() { outputColor = vec4(basis[0][0]); }

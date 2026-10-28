#version 450
layout(set=0,binding=0,rgba32f) readonly uniform image2DArray pixels;
layout(location=0) out vec4 color;
void main(){color=imageLoad(pixels,ivec3(0));}

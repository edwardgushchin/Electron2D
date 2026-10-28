#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform sampler2DArray pixels;
void main(){result=texelFetch(pixels,ivec3(1,1,textureSize(pixels,0).z-1),0)*color;}

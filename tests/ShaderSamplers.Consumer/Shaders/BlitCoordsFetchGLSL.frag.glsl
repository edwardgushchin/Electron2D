#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform sampler2D SOURCE_TEXTURE0;
void main(){ result=texelFetch(SOURCE_TEXTURE0,ivec2(0),1); }

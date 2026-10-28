#version 450
layout(set=0,binding=0) uniform sampler2D source;
layout(location=0) out vec4 color;
void main(){color=texelFetch(source,ivec2(0),0);}

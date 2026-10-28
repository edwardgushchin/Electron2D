#version 450
layout(set=0,binding=0) uniform sampler2D sampled;
layout(set=0,binding=1,r32ui) uniform uimage2D counter;
layout(location=0) out uvec4 integerColor;layout(location=1) out vec4 color;
void main(){color=texelFetch(sampled,ivec2(0),0);integerColor=uvec4(round(color*255));imageAtomicAdd(counter,ivec2(gl_FragCoord.xy),1);}

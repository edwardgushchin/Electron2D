#version 450
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
layout(push_constant) uniform Push { uint step; } pc;
layout(location=0) out vec4 color;
void main(){atomicAdd(value[1],pc.step);color=pc.step==1?vec4(1,0,0,1):vec4(0,1,0,1);}

#version 450
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
void main(){uvec2 p=uvec2(gl_FragCoord.xy); atomicAdd(value[1],1); atomicAdd(value[8+p.x+p.y*7],1);}

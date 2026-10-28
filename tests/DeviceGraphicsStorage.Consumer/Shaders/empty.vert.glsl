#version 450
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
void main(){vec2 p[3]=vec2[](vec2(-1,-1),vec2(3,-1),vec2(-1,3)); atomicAdd(value[0],1); gl_Position=vec4(p[gl_VertexIndex],0,1);}

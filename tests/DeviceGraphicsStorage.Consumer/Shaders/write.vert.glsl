#version 450
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
layout(push_constant) uniform Push { uint step; } pc;
layout(constant_id=0) const uint factor=1;
void main(){atomicAdd(value[0],factor);if(gl_VertexIndex==0 && pc.step==1){for(uint i=0;i<2;i++){value[4+i*4]=3;value[5+i*4]=1;value[6+i*4]=0;value[7+i*4]=0;}}vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

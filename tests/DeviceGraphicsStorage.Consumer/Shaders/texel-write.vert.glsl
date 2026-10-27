#version 450
layout(set=3,binding=7,rgba8)uniform imageBuffer texels;
layout(set=4,binding=2,r32ui)uniform coherent uimageBuffer counter;
layout(push_constant)uniform Push{uint step;}pc;
layout(constant_id=0)const uint factor=1;
void main(){imageStore(texels,gl_VertexIndex,pc.step==1?vec4(1,0,0,1):vec4(0,1,.5,1));imageAtomicAdd(counter,0,factor);vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

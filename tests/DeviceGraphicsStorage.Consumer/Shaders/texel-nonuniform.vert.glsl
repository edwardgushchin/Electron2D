#version 450
#extension GL_EXT_nonuniform_qualifier:require
layout(set=3,binding=7,r32ui)uniform coherent uimageBuffer counters[2];
void main(){imageAtomicAdd(counters[nonuniformEXT(gl_VertexIndex&1)],0,1);vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

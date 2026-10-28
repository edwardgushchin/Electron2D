#version 450
layout(set=0,binding=0,r32ui) uniform uimage2D images[2];
layout(push_constant) uniform Push {uint selected;};
void main(){imageAtomicAdd(images[selected],ivec2(gl_VertexIndex,0),1);vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

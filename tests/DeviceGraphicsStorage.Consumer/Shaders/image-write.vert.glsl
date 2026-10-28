#version 450
layout(set=0,binding=0,r32ui) uniform uimage2D first;
layout(push_constant) uniform Push {uint step;};
void main(){imageStore(first,ivec2(gl_VertexIndex,0),uvec4(step+gl_VertexIndex));vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

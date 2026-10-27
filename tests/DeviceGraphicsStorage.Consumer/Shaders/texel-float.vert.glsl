#version 450
layout(set=3,binding=7,rgba32f)writeonly uniform imageBuffer texels;layout(push_constant)uniform Push{vec4 value;}pc;
void main(){imageStore(texels,gl_VertexIndex,pc.value);vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

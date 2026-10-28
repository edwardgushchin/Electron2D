#version 450
layout(set=0,binding=5,r32ui) readonly uniform uimage2D checkImage;
void main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,imageLoad(checkImage,ivec2(0)).r==3?0:2,1);}

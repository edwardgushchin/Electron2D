#version 450
layout(set=0,binding=0,rgba32f) readonly uniform image2DArray pixels;
void main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,imageSize(pixels).z==1&&imageLoad(pixels,ivec3(0)).a==1?0:2,1);}

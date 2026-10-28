#version 450
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
layout(set=0,binding=1) uniform sampler2D source;
void main(){atomicAdd(value[0],uint(texelFetch(source,ivec2(0),0).r));vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}

#version 450
layout(set=3,binding=7,rgba32i)writeonly uniform iimageBuffer texels;layout(push_constant)uniform Push{ivec4 value;}pc;layout(location=0)out vec4 color;
void main(){int i=int(gl_FragCoord.x)+int(gl_FragCoord.y)*4;imageStore(texels,4+i,pc.value);color=vec4(1);}

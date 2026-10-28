#version 450
layout(local_size_x=4,local_size_y=4) in;
layout(set=0,binding=0,rgba32f) uniform image2D destination;
void main(){ivec2 p=ivec2(gl_GlobalInvocationID.xy);imageStore(destination,p,imageLoad(destination,p)+vec4(.125,0,0,0));}

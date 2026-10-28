#version 450
layout(local_size_x=1,local_size_y=1,local_size_z=1) in;
layout(set=0,binding=0,rgba32f) writeonly uniform image2DArray pixels;
void main(){ivec3 p=ivec3(gl_GlobalInvocationID);if(all(lessThan(p,imageSize(pixels))))imageStore(pixels,p,vec4(float(p.z+1)*.2,.4,.8,1));}

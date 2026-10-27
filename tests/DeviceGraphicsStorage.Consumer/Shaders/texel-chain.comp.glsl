#version 450
layout(local_size_x=1)in;
layout(set=3,binding=7,rgba8)uniform imageBuffer texels;
layout(set=4,binding=2,r32ui)uniform uimageBuffer counter;
void main(){imageStore(texels,3,vec4(1,0,0,1));imageStore(counter,2,uvec4(imageLoad(counter,0).x+imageLoad(counter,1).x));}

#version 450
layout(local_size_x=1)in;
layout(set=0,binding=0,rgba32f)uniform readonly image2D image0;
layout(set=1,binding=0,std430)buffer Output{vec4 result;};
void main(){result=imageLoad(image0,ivec2(0));}

#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
void main(){value[2]=value[0]+value[1];}

#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std430) buffer Result { float value[]; } output_data;
void main(){ output_data.value[0]=3.25; }

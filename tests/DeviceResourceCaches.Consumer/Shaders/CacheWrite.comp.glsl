#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std430) buffer Result { uint value; } output_data;
layout(set=0,binding=1,std140) uniform Input { uvec4 value; } input_data;
void main() { output_data.value=input_data.value.x+7; }

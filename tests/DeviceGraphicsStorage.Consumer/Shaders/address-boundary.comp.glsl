#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std140) uniform Input { uvec4 values[256]; } inputData;
layout(set=0,binding=1,std430) buffer Output { uvec4 value; } outputData;
void main() { outputData.value = inputData.values[255]; }

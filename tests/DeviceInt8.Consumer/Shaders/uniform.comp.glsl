#version 450
#extension GL_EXT_shader_8bit_storage:require
layout(local_size_x=1)in;
layout(set=0,binding=0,std140)uniform Source { int8_t value; } sourceData;
layout(set=0,binding=1,std430)buffer Target { int value; } targetData;
void main(){targetData.value=int(sourceData.value);}

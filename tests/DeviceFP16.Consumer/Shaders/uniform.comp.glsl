#version 450
#extension GL_EXT_shader_16bit_storage:require
layout(local_size_x=1)in;
layout(set=0,binding=0,std140)uniform Source { float16_t value; } sourceData;
layout(set=0,binding=1,std430)buffer Target { float value; } targetData;
void main(){targetData.value=float(sourceData.value);}

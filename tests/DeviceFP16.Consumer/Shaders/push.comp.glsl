#version 450
#extension GL_EXT_shader_16bit_storage:require
layout(local_size_x=1)in;
layout(push_constant,std430)uniform Push { float16_t value; } pc;
layout(set=0,binding=0,std430)buffer Target { float value; } targetData;
void main(){targetData.value=float(pc.value);}

#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0) uniform sampler2D pixels;
layout(set=1,binding=0,std430) buffer Result { vec4 values[]; } result_data;
void main() { uint i=gl_GlobalInvocationID.x; result_data.values[i]=textureLod(pixels,vec2(0.5),i==0?-1.0:0.25); }

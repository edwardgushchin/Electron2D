#version 450
layout(local_size_x=1) in;
layout(set=2,binding=13) uniform sampler2DArrayShadow depthMap;
layout(set=3,binding=0,std430) buffer Output { float values[]; };
void main() { uint layer=gl_GlobalInvocationID.x; values[layer]=textureGrad(depthMap,vec4(.5,.5,layer,.25),vec2(0),vec2(0)); }

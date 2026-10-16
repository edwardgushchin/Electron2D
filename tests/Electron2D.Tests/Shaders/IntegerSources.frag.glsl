#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform usampler2D values;
layout(set=2,binding=1) uniform usampler2DArray layers;
layout(set=3,binding=0,std140) uniform Params { float selectedLayer; float selectedMip; float exposure; };
void main() {
    uvec3 a=texelFetch(values,ivec2(0),int(selectedMip)).rgb;
    uvec3 b=texelFetch(layers,ivec3(0,0,int(selectedLayer)),int(selectedMip)).rgb;
    result=vec4(vec3(a+b)*exposure,1)*color;
}

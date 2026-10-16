#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform sampler2DArray layers;
layout(set=3,binding=0,std140) uniform Params { float selectedLayer; float selectedMip; float exposure; };
void main() { vec4 value = texelFetch(layers, ivec3(0, 0, int(selectedLayer)), int(selectedMip)); result = vec4(value.rgb * exposure, value.a) * color; }

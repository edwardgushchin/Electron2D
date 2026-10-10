#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform sampler2DArray layers;
layout(set=2,binding=1) uniform sampler2D detailMap;
layout(set=3,binding=0,std140) uniform Params { float selectedLayer; };
void main() { result = texture(layers, vec3(uv, selectedLayer)) * texture(detailMap, uv) * color; }

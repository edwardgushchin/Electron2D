#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
layout(set=2,binding=0) uniform usampler2D values;
layout(set=2,binding=1) uniform isampler2DArray layers;
layout(set=3,binding=0,std140) uniform Params { float selectedLayer; };
void main() { result=vec4(texture(values,uv))/255.0 + vec4(texture(layers,vec3(uv,selectedLayer)))*.01; result.a=1; result*=color; }

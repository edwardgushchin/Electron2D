#version 450
layout(input_attachment_index=0,set=6,binding=7) uniform subpassInputMS source;
layout(location=0) out vec4 color;
void main() { vec4 total=vec4(0);for(int sample_index=0;sample_index<4;sample_index++)total+=subpassLoad(source,sample_index);color=total.bgra*.25; }

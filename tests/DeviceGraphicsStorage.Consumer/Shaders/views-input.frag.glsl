#version 450
layout(input_attachment_index=0,set=6,binding=7) uniform subpassInput source;
layout(location=0) out vec4 color;
vec4 read_color(subpassInput image) { return subpassLoad(image); }
void main() { color=read_color(source).bgra; }

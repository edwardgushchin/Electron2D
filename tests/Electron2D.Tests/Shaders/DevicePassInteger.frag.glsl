#version 450
layout(input_attachment_index=1,set=6,binding=7) uniform isubpassInput signed_source;
layout(input_attachment_index=2,set=3,binding=2) uniform usubpassInput unsigned_source;
layout(location=0) out vec4 color;
void main() { color=vec4(subpassLoad(signed_source).r==-37 ? 1.0 : 0.0,0,subpassLoad(unsigned_source).r==4000000000u ? 1.0 : 0.0,1); }

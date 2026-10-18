#version 450
layout(location=0) in vec4 MODULATE;
layout(location=1) in vec2 UV;
layout(location=0) out vec4 COLOR0;
layout(location=1) out vec4 COLOR1;
layout(set=2,binding=0) uniform sampler2D SOURCE_TEXTURE0;
layout(set=2,binding=1) uniform sampler2D SOURCE_TEXTURE1;
layout(set=2,binding=2) uniform sampler2D detail;
layout(set=3,binding=0,std140) uniform Params { vec4 gain; };
void main() { COLOR0=texture(SOURCE_TEXTURE0,UV)*MODULATE*gain; COLOR1=texture(SOURCE_TEXTURE1,UV)*texture(detail,UV)*gain; }

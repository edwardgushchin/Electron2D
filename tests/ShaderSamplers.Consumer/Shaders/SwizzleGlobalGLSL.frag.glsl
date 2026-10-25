#version 450
layout(set=2,binding=0) uniform sampler2D GLOBAL_swizzle_image;
layout(set=3,binding=0,std140) uniform Values { float gain; };
layout(location=1) in vec2 uv;
layout(location=0) out vec4 result;
void main(){result=texture(GLOBAL_swizzle_image,uv)*gain;}

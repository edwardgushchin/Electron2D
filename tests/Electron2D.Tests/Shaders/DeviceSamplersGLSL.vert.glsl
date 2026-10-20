#version 450
layout(set=3,binding=7) uniform sampler2D image;
layout(location=0) out vec4 tint;
layout(location=1) out vec2 uv;
void main() {
 vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2); gl_Position=vec4(p*2-1,0,1); uv=p;
 tint=textureLod(image,vec2(.75,.25),0);
}

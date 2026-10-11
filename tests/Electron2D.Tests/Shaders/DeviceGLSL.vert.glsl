#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec2 offset;
layout(location=0) out vec4 tint;
layout(set=5,binding=7) uniform Settings { vec4 color; } settings;
layout(set=6,binding=8,std430) readonly buffer Modifiers { vec4 value; } modifier;
void main() { gl_Position=vec4(position+offset,0,1); tint=settings.color*modifier.value; }

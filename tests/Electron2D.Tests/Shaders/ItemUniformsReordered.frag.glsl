#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(set=2,binding=0) uniform sampler2D TEXTURE;
layout(set=3,binding=1,std140) uniform Shared { float scale; };
layout(set=3,binding=0,std140) uniform Instance { uint number; ivec2 channel; bool enabled; vec4 tint; };
layout(location=0) out vec4 outputColor;
void main() { outputColor = color * texture(TEXTURE,uv) * tint * scale * (enabled ? 1.0 : 0.0) * float(channel.x+channel.y) / float(number); }

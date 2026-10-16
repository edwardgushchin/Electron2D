#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Parameters { vec2 offset; float gain; };
layout(set=1,binding=2,std140) uniform Instance { vec2 displacement; float intensity; };
layout(set=0,binding=0) uniform sampler2D TEXTURE;
layout(set=0,binding=1) uniform sampler2D vertexMap;
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    vec2 p = position + offset * gain + displacement;
    p += vec2(textureLod(vertexMap, vec2(.5), 0).r * 4, textureLod(TEXTURE, vec2(.5), 0).r * 4);
    gl_Position = vec4(p.x / size.x * 2.0 - 1.0, 1.0 - p.y / size.y * 2.0, 0, 1);
    outColor = color * intensity;
    outUV = uv;
}

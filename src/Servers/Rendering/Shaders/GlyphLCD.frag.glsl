#version 450
layout(location=1) in vec2 uv;
layout(location=11) flat in vec4 glyph;
layout(location=0) out vec4 e2d_output;
layout(set=2,binding=0) uniform sampler2D E2D_GLYPH_TEXTURE;
void main() {
    vec4 color = e2d_output;
    if (uint(glyph.x) == 3u) color = texture(E2D_GLYPH_TEXTURE, uv).a == 1.0 ? vec4(color.rgb * color.a, color.a) : vec4(0.0);
    e2d_output = color;
}

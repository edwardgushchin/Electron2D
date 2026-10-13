#version 450
layout(location=1) in vec2 uv;
layout(location=11) flat in vec4 glyph;
layout(location=0) out vec4 e2d_output;
layout(set=2,binding=0) uniform sampler2D E2D_GLYPH_TEXTURE;
void main() {
    vec4 color = e2d_output;
    vec4 sample_value = texture(E2D_GLYPH_TEXTURE, uv);
    uint mode = uint(glyph.x);
    if (mode == 3u) {
        color = sample_value.a == 1.0 ? vec4(sample_value.rgb, color.a) : vec4(0.0);
    } else {
        vec2 texture_size = vec2(textureSize(E2D_GLYPH_TEXTURE, 0));
        vec2 destination_size = vec2(1.0) / max(fwidth(uv), vec2(1e-20));
        float pixel_size = clamp(0.5 * dot(vec2(glyph.y) / texture_size, destination_size), 1.0, 1e20);
        float distance_value = max(min(sample_value.r, sample_value.g), min(max(sample_value.r, sample_value.g), sample_value.b));
        if (glyph.z > 0.0 && mode == 2u) {
            float offset_value = clamp(glyph.z, 0.0, max(glyph.y / 2.0 - 1.0, 0.0)) / glyph.y;
            distance_value = min(distance_value, sample_value.a);
            color.a *= clamp((distance_value - 0.5 + offset_value) * pixel_size, 0.0, 1.0);
        } else color.a *= clamp((distance_value - 0.5) * pixel_size + 0.5, 0.0, 1.0);
    }
    e2d_output = color;
}

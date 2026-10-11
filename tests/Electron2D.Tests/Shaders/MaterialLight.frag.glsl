#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 outputColor;
layout(set=2,binding=0) uniform sampler2D TEXTURE;
layout(set=3,binding=0,std140) uniform A { vec4 tint; float threshold; };
layout(set=3,binding=1,std140) uniform B { float scaleB; };
layout(set=3,binding=2,std140) uniform C { float scaleC; };
layout(set=3,binding=3,std140) uniform D { float scaleD; };
void main() {
    if (uv.x < threshold) discard;
    outputColor = color * tint * texture(TEXTURE,uv) * (scaleB+scaleC+scaleD);
    if (uv.y < 0.5) return;
    outputColor.rgb *= 0.5;
}

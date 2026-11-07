#version 450
layout(location=11) noperspective in float shade;
layout(location=0) out vec4 color;

void main() {
    color = vec4(shade, gl_FragCoord.x > 0 ? 0.5 : 0.0, 0.75, 1.0);
}

#version 450
layout(constant_id=0) const double factor=2.0LF;
layout(set=0,binding=0,std140) uniform Parameters { dvec2 inputValue; } parameters;
layout(push_constant,std430) uniform Push { double delta; double fragmentValue; } pc;
layout(location=0) flat out float signalValue;
void main(){
 vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2)*2-1;
 double resultValue=(parameters.inputValue.x-1.0LF)*1099511627776.0LF+pc.delta;
 gl_Position=vec4(p,0,1); signalValue=float(resultValue*factor/16.0LF);
}

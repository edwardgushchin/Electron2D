#version 450
layout(constant_id=0) const double factor=2.0LF;
layout(set=0,binding=0,std140) uniform Parameters { dvec2 inputValue; } parameters;
layout(push_constant,std430) uniform Push { double delta; double fragmentValue; } pc;
layout(location=0) flat in float signalValue;
layout(location=0) out vec4 color;
void main(){
 double resultValue=(parameters.inputValue.y-1.0LF)*1099511627776.0LF+pc.fragmentValue;
 color=vec4(signalValue,float(resultValue*factor/16.0LF),float(factor/8.0LF),1);
}

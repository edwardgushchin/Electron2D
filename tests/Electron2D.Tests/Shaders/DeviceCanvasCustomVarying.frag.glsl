#version 450

layout(location = 12) in vec4 customColor;
layout(location = 0) out vec4 result;

void main()
{
    result = customColor;
}

#version 130
precision highp float;

uniform sampler2D sampler; 
in vec2 texcoord;
out vec4 outputColor;

void main()
{
    vec4 color = texture2D(sampler, texcoord);
	if(color.a <= 0.2)
		discard;
	outputColor = color;
}


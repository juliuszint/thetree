#version 130
precision highp float;

in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

uniform sampler2D displacement_sampler; 
uniform float displacement_scalar;
uniform mat4 modelview_projection_matrix;

out vec2 texcoord;

void main()
{
	texcoord = in_uv;
    vec4 displacement_color = texture2D(displacement_sampler, texcoord);
    vec4 position = vec4(in_position, 1);
	position.y += (0.5 - displacement_color.x) * displacement_scalar;
	gl_Position = modelview_projection_matrix * position;
}
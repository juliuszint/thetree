#version 130
precision highp float;

// input aus der VAO-Datenstruktur
in vec3 in_position;
in vec3 in_normal; 
in vec2 in_uv; 

uniform mat4 modelview_projection_matrix;
out vec2 texcoord;

void main()
{
	texcoord = in_uv;
	gl_Position = modelview_projection_matrix * vec4(in_position, 1);
}


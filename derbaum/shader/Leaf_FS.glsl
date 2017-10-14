#version 130
precision highp float;

uniform sampler2D sampler; 
uniform sampler2D other_texture;
uniform float ratio;

in vec2 texcoord;
out vec4 outputColor;

void main()
{
    vec4 color_0 = texture2D(sampler, texcoord);
    vec4 color_1 = texture2D(other_texture, texcoord);

	float max_r = max(color_0.r, color_1.r);
	float max_g = max(color_0.g, color_1.g);
	float max_b = max(color_0.b, color_1.b);

	float min_r = min(color_0.r, color_1.r);
	float min_g = min(color_0.g, color_1.g);
	float min_b = min(color_0.b, color_1.b);

	float delta_r = max_r - min_r;
	float delta_g = max_g - min_g;
	float delta_b = max_b - min_b;

	outputColor.r = min_r + (delta_r * ratio);
	outputColor.g = min_g + (delta_g * ratio);
	outputColor.b = min_b + (delta_b * ratio);
}

#version 330
precision highp float;

uniform sampler2D color_texture_one;
uniform sampler2D color_texture_two;
uniform sampler2D normalmap_texture;

uniform mat4 model_matrix;

uniform vec3 light_direction;
uniform vec4 light_ambient_color;
uniform vec4 light_diffuse_color;
uniform vec4 light_specular_color;
uniform vec4 camera_position;
uniform float specular_shininess;
uniform float texture_fraction;

in vec2 fragTexcoord;
in mat3 fragTBN;
in vec4 fragPosition;

out vec4 outputColor;

void main()
{
	vec4 surfaceColor_one = texture(color_texture_one, fragTexcoord);
	vec4 surfaceColor_two = texture(color_texture_two, fragTexcoord);
	if(surfaceColor_one.a < 0.2) {
		discard;
	}
	// calculate normal from texture
    vec3 normal = texture(normalmap_texture, fragTexcoord).rgb;
	normal = normalize(normal * 2.0 - 1.0); 
	normal = normalize(fragTBN * normal); 

	// calculate rotation matrix
	mat3 normalMatrix = mat3(model_matrix);
	
	// calculate view direction
	vec4 v = normalize(camera_position - model_matrix * fragPosition);

	// calculate halfway vector
	vec3 h = normalize(light_direction + vec3(v));
	float ndoth = dot(normal, h);
	float specular_intensity = pow(ndoth, specular_shininess);

	// caclulate diffuse_intensity;
	float diffuse_intensity = clamp(dot(normalize(normal), light_direction), 0, 1);

	vec4 surfaceColor = mix(surfaceColor_one, surfaceColor_two, texture_fraction);
	outputColor = vec4(0, 0, 0, 0);
	outputColor += surfaceColor * light_ambient_color;
	outputColor += surfaceColor * light_diffuse_color * diffuse_intensity;
	//outputColor += light_specular_color * specular_intensity;
	outputColor = clamp(outputColor, 0, 1);
}

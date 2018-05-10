R""(
#version 430

layout (location = 0) in vec3 position;
out vec2 fragCoord;

uniform float resolutionWidth;
uniform float resolutionHeight;

void main()
{
	fragCoord.x = (position.x + 1)/2 * resolutionWidth;
	fragCoord.y = (position.y + 1)/2 * resolutionHeight;
    gl_Position = vec4(position, 1);
}
)""
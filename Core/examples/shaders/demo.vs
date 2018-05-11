R""(
#version 430
layout (location = 0) in vec3 position;
out vec2 fragCoord;

void main()
{
	fragCoord.x = (position.x + 1) * 1 / 2.;
	fragCoord.y = (position.y + 1) * 1 / 2.;
	gl_Position = vec4(position, 1);
}
)""
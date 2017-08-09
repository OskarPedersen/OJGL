R"(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform float iGlobalTime;

uniform float CHANNEL_0_TIME_SINCE[41];
uniform float CHANNEL_1_TIME_SINCE[23];
uniform float CHANNEL_11_TIME_SINCE[28];
uniform float CHANNEL_12_TIME_SINCE[2];
uniform float CHANNEL_13_TIME_SINCE[1];

uniform float CHANNEL_0_TIME_TO[41];
uniform float CHANNEL_1_TIME_TO[23];
uniform float CHANNEL_11_TIME_TO[28];
uniform float CHANNEL_12_TIME_TO[2];
uniform float CHANNEL_13_TIME_TO[1];

uniform float CHANNEL_12_TOTAL;
uniform float CHANNEL_13_TOTAL;


float rect(vec2 p, vec2 size)
{
	return max(abs(p.x) - size.x, abs(p.y) - size.y);
}

float rect(vec2 p, float w, float h)
{
	return rect(p, vec2(w, h));
}
void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
	vec2 p = fragCoord;
	vec3 color = vec3(0.2);

	if (rect(p - vec2(0.5, 0.1), 0.02, 0.02 * pow(0.995, CHANNEL_13_TIME_SINCE[0])) < 0.0 ) {
		color = vec3(1.0, 0.0, 0.0);
	}

	if (rect(p - vec2(mod(CHANNEL_13_TOTAL * 0.1 - pow(0.995, CHANNEL_13_TIME_SINCE[0]) * 0.1, 1.0), 0.1), 0.02, 0.02) < 0.0 ) {
		color = vec3(0.7, 0.8, 0.3);
	}

	for (int i = 0; i < 2; i++) {
		if (rect(p - vec2(0.5 - 0.1 + 0.2 * i, 0.25), 0.02, 0.1 * CHANNEL_12_TIME_TO[i] / 1000.0) < 0.0 ) {
			color = vec3(1.0, 1.0, 0.0);
		}
	}

	if (rect(p - vec2(mod(CHANNEL_12_TOTAL * 0.1 - pow(0.995, CHANNEL_12_TIME_SINCE[0]) * 0.1, 1.0), 0.25), 0.02, 0.02) < 0.0 ) {
		color = vec3(0.2, 0.3, 0.7);
	}

	for (int i = 0; i < 28; i++) {
		if (rect(p - vec2(0.5 - 0.45 + 0.01 * i, 0.40), 0.005, 0.05 * pow(0.995, CHANNEL_11_TIME_SINCE[i])) < 0.0 ) {
			color = vec3(1.0, 0.0, 1.0);
		}
	}

	for (int i = 0; i < 23; i++) {
		if (rect(p - vec2(0.5 - 0.45 + 0.01 * i, 0.55), 0.005, 0.05 * pow(0.995, CHANNEL_1_TIME_SINCE[i])) < 0.0 ) {
			color = vec3(0.3, 1.0, 0.5);
		}
	}

	for (int i = 0; i < 41; i++) {
		if (rect(p - vec2(0.5 - 0.45 + 0.01 * i, 0.70), 0.005, 0.05 * pow(0.995, CHANNEL_0_TIME_SINCE[i])) < 0.0 ) {
			color = vec3(0.5, 0.0, 0.5);
		}
	}

	fragColor = vec4(color, 1.0);
}

void main()
{
	mainImage(fragColor, fragCoord);
}

)"
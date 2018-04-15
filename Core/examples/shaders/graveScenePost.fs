R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform float iGlobalTime;

uniform float CHANNEL_11_SINCE;
uniform float CHANNEL_4_TO;
uniform float CHANNEL_4_TOTAL;

void main()
{
	vec2 uv = fragCoord.xy;

	fragColor = texture(inTexture0, uv);
	
	float red = clamp(2.0 - CHANNEL_11_SINCE, 0.0, 1.0);
	//fragColor.rgb = mix(vec3(1,0,0), fragColor.rgb, min(red, 1.0));

	float a = 0.5 + 0.5 * sin(iGlobalTime* 5.0);
	//a = a * a * a;
	a = 2.0 * red + red * sin(iGlobalTime * 100.0); //0.6
	vec2 dir = normalize(uv - vec2(0.5));
	float l = length(vec2(0.5) - uv);
	fragColor.g = texture(inTexture0, uv + dir * a * 0.01*l).g;
	fragColor.b = texture(inTexture0,  uv + dir * a * 0.02*l).b;

	fragColor.rgb *=  0.9 + 0.1*clamp(sin(uv.y*1000) + 0.8, 0.0, 1.0);
    
	//fragColor.rgb = vec3(sin(iGlobalTime*1000.0));

	if (CHANNEL_4_TO > 0.0 && CHANNEL_4_TOTAL < 0.5) {
		fragColor.rgb = vec3(1,0,0);
	} else {
		fragColor.rgb = vec3(1,0,1);
	}
		
}
)""
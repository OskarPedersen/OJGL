R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;

void main()
{
	vec2 uv = fragCoord.xy;// / iResolution.xy;
    
	if (uv.x < 0.5) {
		fragColor.rgb = texture(inTexture2, uv).rgb;
	} else {
		fragColor.rgb = texture(inTexture0, uv).rgb;
		//fragColor.rgb = vec3(1, 0, 0);
	}
	//fragColor.a = 1.0;
	fragColor.rgb = texture(inTexture2, uv).rgb;

	//fragColor.rgb /= (fragColor.rgb + vec3(1.0));

	float a = 0.5;
	vec2 dir = normalize(uv - vec2(0.5));
	float l = length(vec2(0.5) - uv);
	fragColor.g = texture(inTexture2, uv + dir * a * 0.01*l).g;
	fragColor.b = texture(inTexture2,  uv + dir * a * 0.02*l).b;

	//fragColor.rgb *=  0.95 + 0.05*clamp(sin(uv.y*1000) + 0.8, 0.0, 1.0);

	
}

)""
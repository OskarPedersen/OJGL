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
	//fragColor.r = texture(iChannel0, uv).a;

	//fragColor.rgb /= (fragColor.rgb + vec3(1.0));

	
}

)""
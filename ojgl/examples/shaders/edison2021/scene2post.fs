R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform sampler2D inTexture1;

uniform vec2 iResolution;

void main()
{
	vec2 uv = fragCoord.xy;
	if (texture(inTexture1, uv).g > 0.1) {
		fragColor = texture(inTexture1, uv) * 3.0;
	} else {
		fragColor = texture(inTexture0, uv);
		//fragColor.rgb = vec3(1.0, 0.0, 0.0);
		fragColor = mix(texture(inTexture0, uv), texture(inTexture1, uv), texture(inTexture1, uv).g * 4.0);
	}

	float alpha = texture(inTexture1, uv).a;
	//alpha = 1.0;
	fragColor = texture(inTexture0, uv) * (1.0 - alpha) + texture(inTexture1, uv) *5.0 * alpha;

	//fragColor.rgb = vec3(texture(inTexture1, uv).g);

	//fragColor = texture(inTexture0, uv) + texture(inTexture1, uv);
	//fragColor /= 2;

	//fragColor.rgb /= (fragColor.rgb + vec3(1.0));

    //fragColor = vec4(pow(fragColor.rgb, vec3(0.4545)), 1.0);


	//fragColor = texture(inTexture0, uv);
	//fragColor.a = 1.0;
}

)""

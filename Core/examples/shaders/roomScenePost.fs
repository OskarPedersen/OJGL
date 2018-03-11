R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform float iGlobalTime;

void main()
{
	vec2 uv = fragCoord.xy;
    /*if (uv.x < 0.5) {
        fragColor.rgb = vec3(1.0, 0.0, 0.0);
        fragColor.a = 1.;
    } else {
    	fragColor = texture(inTexture0, uv);
    }*/
	fragColor = texture(inTexture0, uv);
	if (iGlobalTime > 25) {
		float a = 0.5 + 0.5 * sin(iGlobalTime* 5.0);
		a = a * a * a;
		vec2 dir = normalize(uv - vec2(0.5));
		float l = length(vec2(0.5) - uv);
		//l = 0.5;
		//a = 1.0;
		fragColor.g = texture(inTexture0, uv + dir * a * 0.05*l).g;
		fragColor.b = texture(inTexture0,  uv + dir * a * 0.1*l).b;

		//fragColor.rgb = vec3(dir.y);
	} 
    
    //fragColor = texture(inTexture0, uv);

	
}
)""
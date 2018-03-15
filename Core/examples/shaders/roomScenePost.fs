R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform float iGlobalTime;

void main()
{
	vec2 uv = fragCoord.xy;
	/*float u = uv.x*2.0 - 1.0;
	float v = uv.y*2.0 - 1.0;
	float r = length(vec2(uv.x,uv.y));

	 float k1 = 0;
	 float k2 = 1.8;
	 float k3 = 0.1;
	  u = u * (1 + 0.001 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r);
	 v = v * (1 + 0.001 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r);
	 uv.x = (u + 1.0) * 0.5;
	 uv.y = (v + 1.0) * 0.5;*/
    /*if (uv.x < 0.5) {
        fragColor.rgb = vec3(1.0, 0.0, 0.0);
        fragColor.a = 1.;
    } else {
    	fragColor = texture(inTexture0, uv);
    }*/
	fragColor = texture(inTexture0, uv);
	//if (iGlobalTime > 15) {
		float a = 0.5 + 0.5 * sin(iGlobalTime* 5.0);
		a = a * a * a;
		a = 1.0;
		vec2 dir = normalize(uv - vec2(0.5));
		float l = length(vec2(0.5) - uv);
		//l = 0.5;
		//a = 1.0;
		fragColor.g = texture(inTexture0, uv + dir * a * 0.01*l).g;
		fragColor.b = texture(inTexture0,  uv + dir * a * 0.02*l).b;

		//fragColor.rgb = vec3(dir.y);
	//} 
    
    //fragColor = texture(inTexture0, uv);

	fragColor.rgb *=  0.7 + 0.3*clamp(sin(uv.y*1000) + 0.8, 0.0, 1.0);

	
}
)""
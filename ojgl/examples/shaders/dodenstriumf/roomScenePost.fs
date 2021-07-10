R""(

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;
uniform float iTime;

void main()
{
	vec2 uv = fragCoord.xy;

	fragColor = texture(inTexture0, uv);

		float a = 0.5 + 0.5 * sin(iTime* 5.0);
		a = a * a * a;
		a = 1.0;
		vec2 dir = normalize(uv - vec2(0.5));
		float l = length(vec2(0.5) - uv);

		fragColor.g = texture(inTexture0, uv + dir * a * 0.01*l).g;
		fragColor.b = texture(inTexture0,  uv + dir * a * 0.02*l).b;



	fragColor.rgb *=  0.7 + 0.3*clamp(sin(uv.y*1000) + 0.8, 0.0, 1.0);

	
}
)""
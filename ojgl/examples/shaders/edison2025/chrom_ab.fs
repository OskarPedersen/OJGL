R""(

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;

uniform float C_1_S;

void main()
{
	vec2 uv = fragCoord.xy;

    vec2 dir = normalize(uv - vec2(0.5));
    float l = length(vec2(0.5) - uv) * (0.1 + 1.0 - pow(C_1_S, 0.1)) * 5.0;




    fragColor.r = texture(inTexture0, uv).r;
    vec2 uv2 = clamp(uv + dir * 0.004*l, vec2(0.001), vec2(0.999));
    vec2 uv3 = clamp(uv + dir * 0.008*l, vec2(0.001), vec2(0.999));
    fragColor.g = texture(inTexture0, uv2).g;
    fragColor.b = texture(inTexture0, uv3).b;


}

)""
R""(

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;

uniform float CHANNEL_4_SINCE; // kick drum

void main()
{
	vec2 uv = fragCoord.xy;

    vec2 dir = normalize(uv - vec2(0.5));
    float l = length(vec2(0.5) - uv) * (0.1 + 1.0 - pow(CHANNEL_4_SINCE, 0.1)) * 5.0;

    //uv.x = uv.x + dir.x * l * l * l * .5;


    fragColor.r = texture(inTexture0, uv).r;
    vec2 uv2 = clamp(uv + dir * 0.004*l, vec2(0.001), vec2(0.999));
    vec2 uv3 = clamp(uv + dir * 0.008*l, vec2(0.001), vec2(0.999));
    fragColor.g = texture(inTexture0, uv2).g;
    fragColor.b = texture(inTexture0, uv3).b;

    //fragColor.rgb *=  0.7 + 0.3*clamp(sin(uv.y*1000) + 0.8, 0.0, 1.0);
}

)""

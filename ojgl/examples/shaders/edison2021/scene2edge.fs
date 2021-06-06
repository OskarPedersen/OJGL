R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;

uniform vec2 iResolution;

void main()
{
	vec2 uv = fragCoord.xy;
    float g = 0.0;
    g += texture(inTexture0, uv).a;
    g += texture(inTexture0, uv + vec2(1.0, 0.0) /  iResolution.x).a;
    g += texture(inTexture0, uv + vec2(-1.0, 0.0) /  iResolution.x).a;
    g += texture(inTexture0, uv + vec2(0.0, 1.0) /  iResolution.y).a;
    g += texture(inTexture0, uv + vec2(0.0, -1.0) /  iResolution.y).a;

    //fragColor.rgb = vec3(g / 5.0);

    if (g > 0.0 && g < 5.0) {
        fragColor.rgb = vec3(0.0, 1.0, 0.0);
        fragColor.a = 1.0;
    } else if (g == 5.0) {
        fragColor.rgb = vec3(1.0);
        fragColor.a = 1.0;
    } else {
        fragColor.rgb = vec3(0.0);
        fragColor.a = 0.0;
    }

   
}

)""

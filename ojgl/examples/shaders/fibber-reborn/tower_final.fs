R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D iChannel0;

void main()
{
	vec2 uv = fragCoord.xy;// / iResolution.xy;
    
    fragColor = texture(iChannel0, uv);
	//fragColor.r = 1.0;
}

)""
R""(

#include "common/noise.fs"

in vec2 fragCoord;
out vec4 fragColor;

void main()
{
	vec2 uv = fragCoord.xy;
    float n = hash12(uv);
    if (n < 0.9985) {
        n = 0;
    }
	fragColor = vec4(n, n, n, 1.0);
}
)""

R""(


uniform sampler2D feedbackTexture0;
uniform sampler2D ojTexture;
uniform float iTime;
uniform float iPreviousTime;
uniform vec2 iResolution;
uniform float x;
uniform float y;

in vec2 fragCoord;
out vec4 fragColor;

float remap(float value, float low1, float high1, float low2, float high2) {
    return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
}

void main()
{
    float u = (fragCoord.x);
    float v = (fragCoord.y);
    vec2 uv = vec2(u, v);
    vec3 color = vec3(0.0);
    uv *= 3.0;
    float dx = 0.01;
    float xx = remap(x * 1.01, -1.0, 1.0, -1.05, 1.0);
    float yy = remap(y * 1.01, -1.0, 1.0, -1.15, 1.2);
    uv.x += (xx-1);
    uv.y += (yy-1);
    if (uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1) {
        color = vec3(1.0) - texture(ojTexture, uv).xyz;
    }
    fragColor = vec4(color, 1);
}
)""

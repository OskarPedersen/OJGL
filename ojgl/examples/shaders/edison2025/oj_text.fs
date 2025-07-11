R""(


uniform sampler2D feedbackTexture0;
uniform sampler2D ojTexture;
uniform float iTime;
uniform float iPreviousTime;
uniform vec2 iResolution;
in vec2 fragCoord;
out vec4 fragColor;

void main()
{
    float u = (fragCoord.x);
    float v = (fragCoord.y);
    vec2 uv = vec2(u, v);
    vec3 color = vec3(0.0);
    uv *= 3.0;
    float dx = 0.01;
    uv.x += min(dx, sin(iTime)-1);
    uv.y += sin(iTime*0.5)-1;
    // uv.y -= 0.5;
    if (uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1) {
        color = vec3(1.0) - texture(ojTexture, uv).xyz;
    }
    fragColor = vec4(color, 1);
}
)""

R""(


uniform sampler2D feedbackTexture0;
uniform float iTime;
uniform float iPreviousTime;
uniform vec2 iResolution;
uniform float C_1_S;
uniform float C_7_S;
uniform float C_1_T;
uniform float C_7_T;

in vec2 fragCoord;
out vec4 fragColor;

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5);
    vec2 uv = vec2(u, v);

    float k = 0.8;
    float kTime = 0.2*C_7_T + iTime;
    float t0 = k * kTime;
    float t1 = k * (kTime) + 0.3;

    const int steps = 20;

    // float t = 0.3*iTime;
    float A = 0.5;
    float B = 0.5;
    float delta = 3.14159265 / 2;
    float a = 3;
    float b = 2;

    float minDist = 1000.0;

    for (int i = 0; i < steps; i++) {
        float t = mix(t0, t1, float(i) / float(steps - 1));
       float x = 0.5 * (A * sin(a * t + delta));
        float y = 0.5 * (B * sin(b * t));
        vec2 curvePos = vec2(x, y);

        float d = distance(uv, curvePos);
        minDist = min(minDist, d);
    }

    float lineWidth = 0.01;
    float alpha = smoothstep(lineWidth, 0.0, minDist);
    vec3 color = mix(vec3(0.0), vec3(1.0, 0.0, 0.0), alpha);
    vec3 prevColor = texture(feedbackTexture0, fragCoord).xyz;
    color = min(color + prevColor, vec3(1.0));

    float tau = 0.7;
    float dt = iTime - iPreviousTime;
    float emaAlpha = 1 - exp(-dt / tau);
    color = mix(color, vec3(0.0), emaAlpha);
    fragColor = vec4(color, 1);
}
)""

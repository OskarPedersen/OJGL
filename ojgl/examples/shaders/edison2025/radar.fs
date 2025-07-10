R""(
#include "common/utils.fs"

uniform sampler2D ojTexture;
uniform sampler2D feedbackTexture0;
uniform float iTime;
uniform float iPreviousTime;
uniform vec2 iResolution;
in vec2 fragCoord;
out vec4 fragColor;

struct DistanceInfo {
    float distance;
    float lineWidth;
};

DistanceInfo un(DistanceInfo a, DistanceInfo b) { return a.distance < b.distance ? a : b; }

const float rw = 1.5;

float circles(vec2 uv)
{
    float q = length(uv);
    if (q > 0.5) {
        return 0.5;
    }
    q = mod(q, 0.1);
    return abs(q - 0.05);
}

float lines(vec2 uv) {
    pModPolar(uv, 12);
    uv = uv.yx;
    float q = length(uv);
    if (q > 0.45 || q < 0.05) {
        return 0.5;
    }
    return abs(uv.x);
}

float radar(vec2 uv) {
    vec2 uvo = uv;
    float dmin = 5.0;
    for(int i = 0; i < 5; i++) {
        float d = 5.0;
        uv = uvo;
        uv *= rot(-rw*iTime + 0.01*i);
        float q = length(uv);
        if (q > 0.45 || uv.y < 0.01) {
            d = 0.5;
        } else {
            d = abs(uv.x);
        }
        dmin = min(d, dmin);

    }
    return dmin;
}

float blip(vec2 uv) {
    uv.x -= 0.1;
    uv.y -= 0.385;
    float s = 0.03 * cbeat(iTime -0.4, 2 * PI / rw, 130.0);

    if (iTime < 7.0) {
        s = 0.0;
    }
    return length(uv) - s;
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5);
    vec2 uv = vec2(u, v);

    DistanceInfo dc = {circles(uv), 0.005};
    DistanceInfo dl = {lines(uv), 0.005};
    DistanceInfo dr = {radar(uv), 0.01};
    DistanceInfo db = {blip(uv), 0.00};
    DistanceInfo d = un(dc, dl);
    d = un(d, dr);
    d = un(d, db);
    float alpha = smoothstep(d.lineWidth, 0.0, d.distance);
    vec3 color = mix(vec3(0.0), vec3(1.0, 0.0, 0.0), alpha);


    vec3 prevColor = texture(feedbackTexture0, fragCoord).xyz;
    color = min(color + prevColor, vec3(1.0));

    float tau = 0.3;
    float dt = iTime - iPreviousTime;
    float emaAlpha = 1 - exp(-dt / tau);
    color = mix(color, vec3(0.0), emaAlpha);
    fragColor = vec4(color, 1.0);
}

)""

R""(
#version 430

// First blur
#define dir vec2(1., 0.)

in vec2 fragCoord;
out vec4 fragColor;

uniform sampler2D inTexture0;

void main()
{
    vec2 uv = fragCoord.xy;// / iResolution.xy;
    vec3 col = vec3(0.);
    float samples = 20.;
    float totalWeight = 0.;
    float focus = texture(inTexture0, uv).a;
    float dist = focus * 0.01;
    for (float i = 0.; i < samples; i++) {
        float f = (i - samples / 2.) / (samples / 2.);
        float weight = 1. - pow(abs(f), 4.);
        float sampleFocus = texture(inTexture0, uv + dir * f * dist).a;
        if (sampleFocus >= focus) {
            weight *= max(0., 1. - abs(sampleFocus - focus) * 0.2);
        }
        
        totalWeight += weight;
        
        col += texture(inTexture0, uv + dir * f * dist).rgb * weight;
    }
    fragColor.rgb = col / totalWeight; //mix( texture(inTexture0, uv).rgb, col / 20., texture(inTexture0, uv).a);
    fragColor.a = focus;
}

)""
R""(
#version 430

in vec2 fragCoord;
out vec4 fragColor;

float hash( in vec2 p ) {
	float h = dot(p,vec2(127.1,311.7));	
    return fract(sin(h)*43758.5453123);
}

float noise( in vec2 p ) {
    vec2 i = floor( p );
    vec2 f = fract( p );	
	vec2 u = f*f*(3.0-2.0*f);
    return mix( mix( hash( i + vec2(0.0,0.0) ), 
                     hash( i + vec2(1.0,0.0) ), u.x),
                mix( hash( i + vec2(0.0,1.0) ), 
                     hash( i + vec2(1.0,1.0) ), u.x), u.y);
}

float noiseOctave(in vec2 p, int octaves, float persistence)
{
	float n = 0.;
	float amplitude = 1.;
	float frequency = 1.;
	float maxValue = 0.;

	for(int i = 0; i < octaves; i++)
	{
		n += noise((p+float(i)) * frequency) * amplitude;
		maxValue += amplitude;
		amplitude *= persistence;
		frequency *= 2.0;
	}
	return n / maxValue; 
}
float hash11(float p)
{
    return fract(sin(p * 727.1) * 435.545);
}
float hash12(vec2 p)
{
    float h = dot(p, vec2(127.1, 311.7));
    return fract(sin(h) * 437.545);
}
vec3 hash31(float p)
{
    vec3 h = vec3(127.231, 491.7, 718.423) * p;
    return fract(sin(h) * 435.543);
}
float noise_2(in vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash12(i + vec2(0.0, 0.0)), hash12(i + vec2(1.0, 0.0)), u.x), mix(hash12(i + vec2(0.0, 1.0)), hash12(i + vec2(1.0, 1.0)), u.x), u.y);
}

void main()
{
    // Normalized pixel coordinates (from 0 to 1)
    //vec2 uv = fragCoord/iResolution.xy;
	vec2 uv = fragCoord;

    if (uv.x > 0.5) {
    	uv.x = 1.0 - uv.x;
    }
    
    if (uv.y > 0.5) {
    	uv.y = 1.0 - uv.y;
    }

    // Output to screen
    //fragColor = vec4(vec3(noiseOctave(uv * 10.0, 20, 0.6)),1.0);

	fragColor = vec4(vec3(noise_2(uv)),1.0);
}

)""
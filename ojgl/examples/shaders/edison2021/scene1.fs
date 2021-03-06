R""(
#version 430
#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/raymarch_settings.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

uniform float CHANNEL_0_SINCE;

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;

const int sphereType = 1;
const int wallType = 2;

DistanceInfo map(in vec3 p)
{
    DistanceInfo walls = { -sdBox(p, vec3(2.0, 2.0, 4.0)), wallType };
    DistanceInfo sphere2 = { sdSphere(p - vec3(1.0, 1.0, -1.0), 0.1), sphereType };

    p.xy *= rot(iTime);
    pModPolar(p.xy, 5);
    p.x -= 0.6;
    float r = max(0.05, 0.25 - CHANNEL_0_SINCE);
    DistanceInfo sphere1 = { sdSphere(p, r) + 0.05 * noise_3(p.xyz * 10.0 + iTime), sphereType };
    return un(un(sphere1, sphere2), walls);
}

float getReflectiveIndex(int type)
{
    return type == wallType ? 0.3 : 0.0;
}

vec3 getColor(in MarchResult result)
{
    vec3 lightPosition = vec3(0, 0, 0);
    if (result.type != invalidType) {
        vec3 ambient = vec3(0.1, 0.1, 0.1 + 0.5 * sin(result.type + 1));
        vec3 invLight = normalize(lightPosition - result.position);
        vec3 normal = normal(result.position);
        float shadow = shadowFunction(result.position, lightPosition, 32);
        float diffuse = max(0., dot(invLight, normal)) * (shadow);
        return vec3(ambient * (0.04 + 0.96*diffuse)) * result.transmittance + result.scatteredLight;
    } else {
        return vec3(0.0);
    }
}

VolumetricResult evaluateLight(in vec3 p)
{
    float d = sdBox(p, vec3(0.1, 0.5, 0.1));

	d = max(0.001, d);

	float strength = 5;
	vec3 col = vec3(1.0, 0.01, 0.01);
	vec3 res = col * strength / (d * d);
	return VolumetricResult(d, res);
}

float getFogAmount(in vec3 p) 
{
    return 0.02;
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -1.0, 1.0)).xyz;
    vec3 eye = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 rayDirection = normalize(rayOrigin - eye);

    vec3 color = march(rayOrigin, rayDirection);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.4545)), 1.0);

}

)""

R""(
#version 430
#include "common/noise.fs"
#include "common/primitives.fs"

const float S_distanceEpsilon = 2e-3;
const float S_normalEpsilon = 1e-2;
const int S_maxSteps = 100;
const float S_maxDistance = 400.0;
const float S_distanceMultiplier = 1.0;
const float S_minVolumetricJumpDistance = 0.02;
const float S_volumetricDistanceMultiplier = 0.75;
const int S_reflectionJumps = 1;

#define S_VOLUMETRIC 0
#define S_REFLECTIONS 1


//include "common/raymarch_utils.fs"

const int invalidType = -1;

struct DistanceInfo {
    float distance;
    int type;
};

struct MarchResult {
    int type;
    vec3 position;
    int steps;
    float transmittance;
    vec3 scatteredLight;
};

struct VolumetricResult {
    float distance;
    vec3 color;
};

DistanceInfo map(in vec3 p);
VolumetricResult evaluateLight(in vec3 p);
float getFogAmount(in vec3 p);
vec3 getColor(in MarchResult result);
float getReflectiveIndex(int type);

vec3 normal(in vec3 p)
{
    vec3 n = vec3(map(vec3(p.x + S_normalEpsilon, p.y, p.z)).distance, map(vec3(p.x, p.y + S_normalEpsilon, p.z)).distance, map(vec3(p.x, p.y, p.z + S_normalEpsilon)).distance);
    return normalize(n - map(p).distance);
}

float shadowFunction(in vec3 hitPosition, in vec3 lightPosition, float k)
{
    float res = 1.0;

    float t = S_distanceEpsilon * 10.0;
    vec3 dir = lightPosition - hitPosition;
    float maxDistance = length(dir);
    dir = normalize(dir);
    while (t < maxDistance) {
        float h = map(hitPosition + dir * t).distance;

        if(h < S_distanceEpsilon)
            return 0.0;
        
        res = min( res, k*h/t );

        t += h;
    }
    return res;
}

DistanceInfo un(DistanceInfo a, DistanceInfo b) { return a.distance < b.distance ? a : b; }

vec3 march(in vec3 rayOrigin, in vec3 rayDirection, out int type)
{
    float t = 0.0;
    vec3 scatteredLight = vec3(0.0);
    float transmittance = 1.0;
    float reflectionModifier = 1.0;
    vec3 resultColor = vec3(0.0);

#if S_REFLECTIONS
    for (int jump = 0; jump < S_reflectionJumps; jump++) {
#endif
        for (int steps = 0; steps < S_maxSteps; ++steps) {
            vec3 p = rayOrigin + t * rayDirection;
            DistanceInfo info = map(p);
            type = info.type;
            float jumpDistance = info.distance * S_distanceMultiplier;

#if S_VOLUMETRIC
            float fogAmount = getFogAmount(p);
            VolumetricResult vr = evaluateLight(p);

            float volumetricJumpDistance = max(S_minVolumetricJumpDistance, vr.distance * S_volumetricDistanceMultiplier);
            jumpDistance = min(jumpDistance, volumetricJumpDistance);
            vec3 lightIntegrated = vr.color - vr.color * exp(-fogAmount * jumpDistance);
            scatteredLight += transmittance * lightIntegrated;	
            transmittance *= exp(-fogAmount * jumpDistance);      
#endif

            t += jumpDistance;
            if (info.distance < S_distanceEpsilon) {
                vec3 color = getColor(MarchResult(info.type, p, steps, transmittance, scatteredLight));
#if !S_REFLECTIONS
                return color;
#else
                t = 0.0;
                rayDirection = reflect(rayDirection, normal(p));
                rayOrigin = p + 0.1 * rayDirection;

                resultColor = mix(resultColor, color, reflectionModifier);
                reflectionModifier *= getReflectiveIndex(info.type);
                break;
 #endif
            }

            if (t > S_maxDistance) {
                vec3 color = getColor(MarchResult(invalidType, p, steps, transmittance, scatteredLight));
                resultColor = mix(resultColor, color, reflectionModifier);
                return resultColor;
            }
        }
#if S_REFLECTIONS
    }
#endif

    return resultColor;
}

// end 
#include "common/utils.fs"

uniform float CHANNEL_0_SINCE;
uniform float CHANNEL_0_TO;

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;

const int sphereType = 1;
const int wallType = 2;
const int pillarType = 3;

DistanceInfo map(in vec3 p)
{
    DistanceInfo walls = { -sdBox(p, vec3(20.0, 3.0, 20.0)), wallType };

    float h = CHANNEL_0_SINCE * 3.0;

    //vec2 q = p.xz;
    //pMod2(q, vec2(3.0, 5.0));
    //DistanceInfo pillar = { sdCappedCylinder(vec3(q.x, p.y, q.y) - vec3(0, -h + 3.0, 0), vec2(0.05, 0.1 + h)), pillarType };

    pMod1(p.z, 1.0);
    float d1 = sdCappedCylinder(rotateAngle(vec3(0, 0, 1), 2 * CHANNEL_0_TO) * (p.yxz) - vec3(0, 2.5, 0), vec2(0.1, 2.5));
    float d2 = sdCappedCylinder(rotateAngle(vec3(0, 0, 1), -2 * CHANNEL_0_TO) * (p.yxz) - vec3(0, 2.5, 0), vec2(0.1, 2.5));
    DistanceInfo pillar = {smink(d1, d2, 0.5), pillarType};

    return un(pillar, walls);
}

float getReflectiveIndex(int type)
{
    return type == wallType ? 0.6 : 0.3;
}

vec3 getColor(in MarchResult result)
{
    vec3 lightPosition = vec3(2.0, 2.0, 2.0);
    /*if (result.type == pillarType) {
        return vec3(0, 100, 0);
    } else */if (result.type != invalidType) {
        vec3 ambient = vec3(0.1, 0.1, 0.1 + 0.5 * sin(result.type + 1));
        vec3 invLight = normalize(lightPosition - result.position);
        vec3 normal = normal(result.position);
        float shadow = 1.0; //shadowFunction(result.position, lightPosition, 32);
        float diffuse = max(0., dot(invLight, normal)) * (shadow);
        return vec3(ambient * (0.00 + (0.2 - min(CHANNEL_0_TO, CHANNEL_0_SINCE))*diffuse)) * result.transmittance + result.scatteredLight;
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

    int type = 0;
    vec3 color = march(rayOrigin, rayDirection, type);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.4545)), 1.0);

    if (type == pillarType) {
        fragColor.a =  1.0;
    } else {
        fragColor.a =  0.0;
    }


    //fragColor.rgb = mix(vec3(1.0), fragColor.rgb, min(1, CHANNEL_0_TO * 10.0));

}

)""

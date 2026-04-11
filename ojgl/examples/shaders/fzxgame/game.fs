R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 500;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.9;
const float S_minVolumetricJumpDistance = 0.005;
float S_volumetricDistanceMultiplier = 0.8;
const int S_reflectionJumps = 2;

#define S_VOLUMETRIC 0
#define S_REFLECTIONS 1
#define S_REFRACTIONS 0

#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;
uniform vec3 iPlayerPosition;

const int playerType = 1;
const int groundType = 2;
const int pillarType = 3;

DistanceInfo map(in vec3 p)
{
    DistanceInfo ground = { p.y + 1.0, groundType };
    DistanceInfo player = { sdSphere(p - iPlayerPosition, 0.3), playerType };
    pMod1(p.x, 10);
    pMod1(p.z, 10);
    DistanceInfo pillars = { sdSphere(p, 0.1), pillarType };
    return un(ground, un(player, pillars));
}

float getReflectiveIndex(int type)
{
    return type == groundType ? 0.2 : 0.0;
}

vec3 getColor(in MarchResult result)
{
    if (result.type == invalidType) {
        return vec3(1, 0, 1);
    } else if (result.type == playerType) {
        return vec3(1, 0, 0);
    } else if (result.type == groundType) {
        return vec3(0, 0, 1);
    }
}

VolumetricResult evaluateLight(in vec3 p)
{
    return VolumetricResult(1000.0, vec3(0.0));
}

float getFogAmount(in vec3 p)
{
    return 0.0;
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    // vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -1.0, 1.0)).xyz;
    // vec3 eye = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    // vec3 rayDirection = normalize(rayOrigin - eye);

    vec3 rayOrigin = iPlayerPosition + vec3(0, 1, -8);
    vec3 tar = iPlayerPosition; //rayOrigin + vec3(10, -3 ,0 );
        
    vec3 dir = normalize(tar - rayOrigin);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	vec3 up = cross(dir, right);
        
    vec3 rayDirection = normalize(dir + right*u + up*v);


    vec3 color = march(rayOrigin, rayDirection);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.4545)), 1.0);
}

)""

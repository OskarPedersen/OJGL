R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
const float S_volumetricDistanceMultiplier = 0.5;
const int S_reflectionJumps = 2;

#define S_VOLUMETRIC 1
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
uniform sampler2D inTexture0;

const int boatType = 1;
const int mountainType = 2;
const int waterType = 4;

vec3 cameraPosition;
vec3 rayDirection;

vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case boatType:
            return 1.2*vec3(1, 1, 1);
        case mountainType: 
            return 0.0*vec3(0.2, 0.2, 0.1);
        case waterType:
            return vec3(0.1, 0.1, 0.7);
        default:
           return 5*vec3(0, 0.0, 1);
    }
}

vec3 getColor(in MarchResult result)
{
    vec3 lightPosition = vec3(-100, 20, 200);
    vec3 normal = normal(result.position);
    vec3 invLight = normalize(lightPosition - result.position);
    float diffuse = max(0., dot(invLight, normal));
    vec3 ambientColor = getAmbientColor(result.type, result.position, normal);
    vec3 color = ambientColor * (0.02 + 0.98*diffuse);
    vec3 ao = vec3(float(result.steps) / 600);
    float k = max(0.0, dot(rayDirection, reflect(invLight, normal)));
    float spec = 1 * pow(k, 30.0);
    color += spec;
    return result.scatteredLight + result.transmittance *  mix(color, ao, 0.75);
}

float getFogAmount(in vec3 p)
{
    return 0.001;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 center = vec3(0, 15, 0);
    //float d = sdRoundBox(p - vec3(0, 15, 0), vec3(0.5, 0.1, 0.5), 0.1);
    float d1 = sdSphere(p  - center, 2.0);
    //float pModPolar(inout vec2 p, float repetitions);
    
    p -= center;
    pModPolar(p.xz, 15);
    //p.x -= 5;
    //pCap.xz = pCap2;

    float d2 = sdVerticalCapsule(p.yxz - (vec3(0, 0, 0)), 10.5,  0.1);
    //float d2 = sdCylinder(p.zyx, 0.5);

    float d = min(d1, d2);

    float str = 50;
    vec3 res = vec3(1.0, 0.2, 0.1) * str / (d * d);
    return VolumetricResult(d, res); 


    //vec2 pxz = p.xz;
    //pMod2(pxz, vec2(10));
    //p.x = pxz.x;
    //p.z = pxz.y;

    //float d = length(p - vec3(0, 5, 0)) - 0.03;
    //float strength = 500;

    //vec3 res = vec3(1.0, 0.2, 0.1) * strength / (d * d);

    //return VolumetricResult(d, res);
}

float getReflectiveIndex(int type) {
    switch (type) {
        case boatType:
            return 0.0;
        case mountainType:
            return 0.0;
        case waterType:
            return 1.0;
        default:
           return 0.0;
    }
}

float tunnel(in vec3 p)
{
    float d = -sdSphere(vec3(p.xy, 0), 4.0 + 0.5*sin(0.05*p.z));
    return d;
}

float water(in vec3 p)
{
    float d = sdPlane(p, vec4(0, 1, 0, 0)) + 0.002*noise_2(5*p.xz + iTime);
    return d;
}

float mountain(vec3 p)
{
    const float r = max(0, length(p.xz) - 60);
    const float k = 40 * exp(-0.006*r);
    if (p.y > k) {
        return sdPlane(p, vec4(0, 1, 0, k));
    }
	float h = 4*texture(inTexture0, (p.xz)/90.0).x + 
              200*pow(texture(inTexture0, (p.xz)/1600.0).x, 4);

	return p.y - h + 10;
}

float boat(vec3 p)
{
    p.y += 0.05 * sin(iTime);
    p.z += 0.1 * sin(iTime + 3);
    p.x += 0.1 * sin(iTime + 5);
    

    float ffz = p.z > 0.0 ? -4.0 : -7.0;
    float fz = 1.7 - 0.7 * smoothstep(ffz, 2.0, p.y);
    float fx = 0.971*smoothstep(3, 7, abs(p.z));
    float fx2 = 1*smoothstep(-3.0, 2.0, p.y);
    float fy = 0.5*smoothstep(3, 7, p.z);
    
    vec3 p1 = p;
    p1 -= vec3(0, 0.4, 0);
    float hull = sdBox(p1, vec3(2 - fx - fx2, 1.0 + fy, 7 / fz));

    vec3 p2 = p;
    p2.y -= 1.3;
    float wfx = 0.9 * smoothstep(-0.8, 0.8, p2.y);
    float wffy = p2.y < 0 ? 0 : 0.3; 
    float wfy = wffy * smoothstep(2.9, 3.3, abs(p2.z));
    float windows = sdBox(p2, vec3(1.2 - wfx, 0.3 - wfy, 3.3));

    vec3 p3 = p;
    p3.z = abs(p3.z);
    p3.y -= 3;
    p3.z -= 3.6;
    float mast = sdCappedCylinder(p3, vec2(0.08, 2.2));


    vec3 p4 = p;
    p4.y -= 4.4;
    p4.y -= 0.9*smoothstep(0, 5, abs(p.z));
    float line = sdBox(p4, vec3(0.01, 0.01, 3.6));
    
    vec3 p5 = p;
    p5.z = abs(p5.z);
    p5.z -= 4.6;
    p5.y -= 3.2;
    p5.zy *= rot(-1.1);
    float line2 = sdBox(p5, vec3(0.01, 0.01, 2.05));

    line = min(line, line2);

    float h = min(line, min(mast, min(windows, hull)));
    return h;

}

DistanceInfo map(in vec3 p)
{
   DistanceInfo box = {mountain(p), mountainType};
   DistanceInfo sphereInfo = {boat(p), boatType};
   DistanceInfo waterInfo = {water(p), waterType};
   return un(waterInfo, un(box, sphereInfo));
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    vec3 color = march(rayOrigin, rayDirection);
    // color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.5)), 1.0);
}

)""

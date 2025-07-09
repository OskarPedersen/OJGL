R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
float S_volumetricDistanceMultiplier = 0.5;
const int S_reflectionJumps = 5;

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

uniform float C_1_S; // bass
uniform float C_6_S; // "vocals"
uniform float C_7_S; // "synth"

uniform float C_7_S_0;
uniform float C_7_S_1;
uniform float C_7_S_2;
uniform float C_7_S_3;

uniform float C_7_T; // "synth"

const int ufoType = 5;
const int mountainType = 6;

vec3 cameraPosition;
vec3 rayDirection;
vec3 firstRayDirection;


float ufoSpeed = 10.0;

vec3 ufoPos()
{
    return vec3(0, 10, 0);
}


vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case ufoType:
            return vec3(1, 0, 1);
        case mountainType:
            return vec3(1, 0.3, 0.1);
        default:
           return 5*vec3(0, 0.0, 1);
    }
}

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

vec3 getColor(in MarchResult result)
{
    vec3 color = vec3(0);

    
    vec3 lightPosition = vec3(-100, 50, -10); //ufoPos();
    vec3 normal = normal(result.position);
    vec3 invLight = normalize(lightPosition - result.position);
    float diffuse = max(0., dot(invLight, normal));
    vec3 ambientColor = getAmbientColor(result.type, result.position, normal);
    color += ambientColor * (0.02 + 0.98*diffuse);
    float k = max(0.0, dot(rayDirection, reflect(invLight, normal)));
    float spec = 1 * pow(k, 30.0);
    color += spec;

    vec3 ao = vec3(float(result.steps) / 600);
    if (result.type == invalidType) {
        return result.scatteredLight;
    } else {
        return result.scatteredLight + result.transmittance *  mix(color, ao, 0.75);
    }

}

float getFogAmount(in vec3 p)
{
    return 0.01;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 pOrig = p;

    vec3 laserFloorP = p.zyx;

    vec3 ufo = ufoPos();

    p -= ufo;


    float section = pModPolar(p.xz, 16);
    
    float tilt = -p.x*0.35;
    
    float capsuleStr = 5;
    if (mod(section, 4.0) == 0.0) {
        capsuleStr = 5.0 + max(0, 10 - C_7_S_0 * 100);
        tilt *= min(1, C_7_S_0 * 3.0);

    } else if (mod(section, 4.0) == 1.0) {
        capsuleStr = 5.0 + max(0, 10 - C_7_S_1 * 100);
         tilt *= min(1, C_7_S_1 * 3.0);

    } else if (mod(section, 4.0) == 2.0) {
        capsuleStr = 5.0 + max(0, 10 - C_7_S_2 * 100);
         tilt *= min(1, C_7_S_2 * 3.0);

    } else if (mod(section, 4.0) == 3.0) {
        capsuleStr = 5.0 + max(0, 10 - C_7_S_3 * 100);
         tilt *= min(1, C_7_S_3 * 3.0);

    }


    p.y -= tilt;
    float dUfoSpin = sdVerticalCapsule(p.yxz - (vec3(0, 0, 0)), 8,  0.01);


    vec3 color = vec3(0.1, 1, 1);
    vec3 res = color * capsuleStr / (dUfoSpin * dUfoSpin);


    float dHyperSum = 999999;

    


    float finalDis = dUfoSpin;
 

    return VolumetricResult(finalDis, res); 
}

float getReflectiveIndex(int type) {
    switch (type) {
        case ufoType:
            return 1.0;
         case mountainType:
            return 0;
        default:
           return 0.0;
    }
}


float opSubtraction( float d1, float d2 )
{
    return max(-d1,d2);
}


float opIntersection( float d1, float d2 )
{
    return max(d1,d2);
}

float mountainH(vec3 p) // just the height
{
    p.x += 20;
    p.z += 100;
    

	float h = 5*texture(inTexture0, (p.xz)/200.0).x + 
              100*pow(texture(inTexture0, (p.xz)/1000.0).x, 4);
    
	return - h + 10;
}

float mountain(vec3 p)
{
    float h = mountainH(p);
	return p.y + h;
}

float ufo(in vec3 p)
{
    p -= ufoPos();
    float d2 = sdTorus(p - vec3(0, -3, 0), vec2(8.5, 0.5));

    float d1 = length(p) - (2.0 + max(0.5 - C_1_S*3, 0));
    return min(d1, d2);
}

DistanceInfo sunk(DistanceInfo a, DistanceInfo b, float k) {
    DistanceInfo res = a.distance < b.distance ? a : b;
    res.distance = smink(a.distance, b.distance, k);
    return res;
}

DistanceInfo map(in vec3 p)
{
   DistanceInfo ufoInfo = {ufo(p), ufoType};
   DistanceInfo mountainInfo = {mountain(p), mountainType};
   return un(ufoInfo, mountainInfo);
}

struct FullMarchResult {
    vec3 col;
    vec3 firstJumpPos;
};

FullMarchResult march2(in vec3 rayOrigin, in vec3 rayDirection)
{
    float t = 0.0;
    vec3 scatteredLight = vec3(0.0);
    float transmittance = 1.0;
    float reflectionModifier = 1.0;
    vec3 resultColor = vec3(0.0);

    vec3 firstJumpPos = vec3(0.0);

    for (int jump = 0; jump < S_reflectionJumps; jump++) {
        for (int steps = 0; steps < S_maxSteps; ++steps) {
            vec3 p = rayOrigin + t * rayDirection;
            
            if (jump == 0) {
                firstJumpPos = p;
            }

            DistanceInfo info = map(p);
            float jumpDistance = info.distance * S_distanceMultiplier;

            float fogAmount = getFogAmount(p);
            VolumetricResult vr = evaluateLight(p);

            float volumetricJumpDistance = max(S_minVolumetricJumpDistance, vr.distance * S_volumetricDistanceMultiplier);
            jumpDistance = min(jumpDistance, volumetricJumpDistance);

            vec3 lightIntegrated = vr.color - vr.color * exp(-fogAmount * jumpDistance);
            scatteredLight += transmittance * lightIntegrated;	
            transmittance *= exp(-fogAmount * jumpDistance);      

            t += jumpDistance;
            if (info.distance < (S_distanceEpsilon)) {
                vec3 color = getColor(MarchResult(info.type, p, steps, transmittance, scatteredLight, jump, rayDirection));

                t = 0.0;
                rayDirection = reflect(rayDirection, normal(p));
                rayOrigin = p + 0.1 * rayDirection;

                resultColor = mix(resultColor, color, reflectionModifier);
                reflectionModifier *= getReflectiveIndex(info.type);
                break;

            }

            if (t > S_maxDistance || steps == S_maxDistance - 1) {
                vec3 color = getColor(MarchResult(invalidType, p, steps, transmittance, scatteredLight, jump, rayDirection));
                resultColor = mix(resultColor, color, reflectionModifier);
                return FullMarchResult(resultColor, firstJumpPos);
            }
        }
    }

    return FullMarchResult(resultColor, firstJumpPos);
}

void main()
{

    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    float zoom = 1.0;

    u *= zoom;
    v *= zoom;

    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    float focus = 0.0;

    { // set camera
       // vec3 ufo = ufoPos();
       // rayOrigin = vec3(ufo.x - 50, 5, ufo.z);
       // vec3 tar = ufo;
       // 
       // vec3 dir = normalize(tar - rayOrigin);
	   // vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	   // vec3 up = cross(dir, right);
       // 
       // rayDirection = normalize(dir + right*u + up*v);
    
    }
  
    

    firstRayDirection = rayDirection;
    //vec3 color = march(rayOrigin, rayDirection);
    FullMarchResult res = march2(rayOrigin, rayDirection);
    vec3 color = res.col;



     color /= (color + vec3(1.0));


     // focus / blur

      //vec3 ufo = ufoPos();
       //focus = abs(length(res.firstJumpPos - ufo)) * 0.005 + 0.01;
      
      //float t4 = max(0, iTime - P_3);
      //focus = mix(focus, 1 - smoothstep(0, 1, t4), t4); // make clearer as ufo ascends
      

    fragColor = vec4(pow(color, vec3(0.5)), clamp(focus, 0.001, 2.0));
}

)""

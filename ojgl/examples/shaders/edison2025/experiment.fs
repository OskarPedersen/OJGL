R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
const float S_volumetricDistanceMultiplier = 0.5;
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

#define PART_1_SHIP_SPLIT 7
#define PART_2_UFO_MOUNTAIN (PART_1_SHIP_SPLIT + 10)

uniform float C_1_S; // bass
uniform float C_6_S; // "vocals"
uniform float C_7_S; // "synth"

uniform float C_7_S_0;
uniform float C_7_S_1;
uniform float C_7_S_2;
uniform float C_7_S_3;

uniform float C_7_T; // "synth"

const int boatType = 1;
const int mountainType = 2;
const int waterType = 4;
const int ufoType = 5;

vec3 cameraPosition;
vec3 rayDirection;

float mountain(vec3 p); // forward declare
float mountainH(vec3 p); // forward declare


vec3 ufoPos = vec3(mod(iTime * 10.0, 200) - 20 + (iTime > PART_1_SHIP_SPLIT ? -50 : 0), 10 + (iTime > PART_1_SHIP_SPLIT ? 3 : 0), 0);
float boatSplitTime = max(0, iTime - 2.15);

vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case boatType:
            return 1.2*vec3(1, 1, 1);
        case mountainType: 
            return 0.0*vec3(0.2, 0.2, 0.1);
        case waterType:
            return vec3(0.1, 0.1, 0.7);
        case ufoType:
            return vec3(1, 0, 1);
        default:
           return 5*vec3(0, 0.0, 1);
    }
}

vec3 getColor(in MarchResult result)
{
    //vec3 lightPosition = vec3(-100, 20, 200);
    vec3 lightPosition = ufoPos;
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
    return 0.01;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 pStars = p;
    vec2 iStars = pMod2(pStars.xz, vec2(50, 50));
    pStars.y -= 50 + sin(iStars.x * 10) * 10 + sin(iStars.y * 10) * 10;
    float dStars = length(pStars) - 0.1;

    vec3 pOrig = p;

    vec3 laserFloorP = p.zyx;

    float dm = mountainH(p);
    laserFloorP.y +=  dm - 0.5;
    //laserFloorP.y += sin(p.x);;
    float dLaserFloor = sdCylinder(laserFloorP, 0.1);
    dLaserFloor = max(dLaserFloor, -p.x); // cut of behind ship, not needed?
    dLaserFloor = max(dLaserFloor,  p.x - ufoPos.x + 0.5); // cut off in fron of UFO
    
    p -= ufoPos;
    

    float dLaser = sdCylinder(p.xzy, 0.1);
    dLaser = max(dLaser, p.y);


    //p.xz *= rot(iTime * 0.5);
    //p.xz *= rot(min(1.0, C_7_S));
    float section = pModPolar(p.xz, 16);
    
    float tilt = -p.x*0.35;
    
    //float capsuleStr = 5 + max(10 - C_1_S * 100, 0);
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

    vec3 laserColor = vec3(1, 0.1, 0.1);
    float laserStr = 50;
    res += laserColor * laserStr / (dLaser * dLaser);

    float laserFloorDis = abs(ufoPos.x - pOrig.x);
    float laserFloorStr = max(0, 50  - laserFloorDis);
    res += laserColor * laserFloorStr / (dLaserFloor * dLaserFloor);

    float starStr = 10;
    //vec3 starColor = vec3(mod(iStars.x * 0.3, 1), mod(iStars.y * 0.4, 1), 1);
    vec3 starColor = vec3(1);
    res += starColor * starStr / (dStars * dStars);

    float finalDis = dStars;
    finalDis = min(finalDis, dLaserFloor);
    //float finalDis = dLaserFloor;
    finalDis = min(finalDis, laserFloorDis); // think this one cuses the white AO wall, maybe something wrong with it
    finalDis = min(finalDis, dLaser);
    finalDis = min(finalDis, dUfoSpin);

    return VolumetricResult(finalDis, res); 
}

float getReflectiveIndex(int type) {
    switch (type) {
        case boatType:
            return 0.5;
        case mountainType:
            return 0.0;
        case waterType:
            return 1.0;
        case ufoType:
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

float mountainH(vec3 p)
{
    if (iTime > PART_1_SHIP_SPLIT) { // Shift mountains to something which works better for laser
        p.x += 20;
        p.z += 100;

    }
    const float r = max(0, length(p.xz) - 60);
    const float k = 40 * exp(-0.006*r);
    //if (p.y > k) {
     //   return sdPlane(p, vec4(0, 1, 0, k));
    //}
	float h = 4*texture(inTexture0, (p.xz)/90.0).x + 
              200*pow(texture(inTexture0, (p.xz)/1600.0).x, 4);
    
	return - h + 10;
}

float mountain(vec3 p)
{
    if (iTime > PART_1_SHIP_SPLIT) { // Shift mountains to something which works better for laser
        p.x += 20;
        p.z += 100;

    }
    const float r = max(0, length(p.xz) - 60);
    const float k = 40 * exp(-0.006*r);
    if (p.y > k) {
        return sdPlane(p, vec4(0, 1, 0, k));
    }
	float h = 4*texture(inTexture0, (p.xz)/90.0).x + 
              200*pow(texture(inTexture0, (p.xz)/1600.0).x, 4);
    
	return p.y - h + 10;
}

float opSubtraction( float d1, float d2 )
{
    return max(-d1,d2);
}

float mountainLaser(vec3 p)
{
    float dMountain = mountain(p);
    vec3 laserFloorP = p.zyx;
    laserFloorP.y +=  mountainH(p) - 0.5;
    float dLaserFloor = sdCylinder(laserFloorP, 1.0);

    dLaserFloor = max(dLaserFloor, -p.x);
    dLaserFloor = max(dLaserFloor,  p.x - ufoPos.x + 0.5);

	return opSubtraction(dLaserFloor, dMountain);
}

float opIntersection( float d1, float d2 )
{
    return max(d1,d2);
}

float boat(vec3 p) {
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
    p4.y -= -(3.0-abs(p.z*0.8))*boatSplitTime*4.0; // make line fall down
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

float boatSplit(vec3 p, float dir)
{
    p.y += mod(boatSplitTime * 0.3, 5.0);
    p.z -= dir*5;
    p.zy *= rot(dir*boatSplitTime*0.1);
    p.z += dir*5;

    p.xy *= rot(dir*boatSplitTime*0.3);

   float h = boat(p);


    float d = sdBox(p - vec3(0, 0, dir*4.95), vec3(5));
    return max(d, h);
}

float ufo(in vec3 p)
{
    p -= ufoPos;
    // float sdRoundBox(vec3 p, vec3 b, float r)
    float d2 = sdTorus(p - vec3(0, -3, 0), vec2(8.5, 0.5));
    //mat2 rot(float a)
   // p.xz *= rot(iTime);
    //p.xy *= rot(iTime);
    //float d1 = sdRoundBox(p, vec3(2), 1);
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
   DistanceInfo mountainDis = {mountainLaser(p), mountainType};

   DistanceInfo boatFrontDis = { boatSplit(p, 1.0), boatType};
   DistanceInfo boatBackDis = { boatSplit(p, -1.0), boatType};
   DistanceInfo boatDis = un(boatFrontDis, boatBackDis);

   DistanceInfo waterInfo = {water(p), waterType};
   DistanceInfo ufoInfo = {ufo(p), ufoType};
   return un(un(ufoInfo, mountainDis), sunk(waterInfo, boatDis, 0.3));
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

  
    if (iTime < PART_1_SHIP_SPLIT) {
        rayOrigin = vec3(11.1394, 1.31, -10.4126);
        vec3 tar = vec3(1, 1, 1);

        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    } else if (iTime < PART_2_UFO_MOUNTAIN) {
        rayOrigin = vec3(15, 8.28, 20);
        vec3 tar = rayOrigin + vec3(0.5, 0, -0.5);

        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    }

    vec3 color = march(rayOrigin, rayDirection);
    // color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.5)), 1.0);
}

)""

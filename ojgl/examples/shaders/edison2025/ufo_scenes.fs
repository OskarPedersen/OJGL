R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 650.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
float S_volumetricDistanceMultiplier = 0.5;
const int S_reflectionJumps = 3;
float g_MountainHeight = 0.0;

#define S_VOLUMETRIC 1
#define S_REFLECTIONS 1
#define S_REFRACTIONS 0

#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/utils.fs"
#include "edison2025/ufo_raymarch_utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform vec2 iResolution;
uniform mat4 iCameraMatrix;
uniform sampler2D borgilaTexture;
uniform sampler2D inTexture0;
uniform sampler2D inTexture1;

float P_0 = 8;
float P_1 = (P_0 + 12);
float P_2 = (P_1 + 10);
float P_25_D = 6.5;
float P_25 = (P_2 + P_25_D);
float P_3 = (P_25 + 8);
#define P_4 (P_3 + 4)

uniform float C_1_S; // bass
uniform float C_5_S;
uniform float C_6_S; // "vocals"
uniform float C_7_S; // "synth"

uniform float C_7_S_0;
uniform float C_7_S_1;
uniform float C_7_S_2;
uniform float C_7_S_3;

uniform float C_7_T; // "synth"


uniform float C_1_T;
uniform float C_6_T;
uniform float C_3_T;
uniform float C_0_T;
uniform float C_2_T;
uniform float C_5_T;
uniform float C_4_T;

bool willHitText = false;

const int boatType = 1;
const int mountainType = 2;
const int waterType = 4;
const int ufoType = 5;

vec3 cameraPosition;
vec3 rayDirection;
vec3 firstRayDirection;

float mountain(vec3 p); // forward declare


float ufoSpeed = 10.0;

vec3 ufoPos()
{
    float timeBeforePart25 = (ojTime - P_0 - P_25_D);
    if (ojTime < P_0) {
        float y = 65 - smoothstep(-6, 6, ojTime) * 7 * 7;
        //y = max(y, 18);
        return vec3(-70, y, 100);

    } else if (ojTime < P_1) {
        float t = ojTime - P_0;
        return vec3(t * ufoSpeed - 70, 10, 0);

    } else if (ojTime < P_2) {
         float t = ojTime - P_0;
        return vec3(t * ufoSpeed - 120, 13, 0);

    } else if (ojTime < P_25) {
        float t = (ojTime - P_2);
        return vec3(t * ufoSpeed - 120, 13, 0);

    } else if (ojTime < P_3) {
        float t = ojTime - P_25;
        return vec3(t * ufoSpeed - 120 - (8 + 6.5)*ufoSpeed, 5, 0);

    } else {
        float t = ojTime - P_3;
        float t2 = ojTime - P_25;
        return vec3(t2 * ufoSpeed - 120 - (8 + 6.5)*ufoSpeed, 5 + t*t*t*t, 0);
    }
}

float boatSplitTime = max(0, ojTime - 7.15 - P_0);


vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case boatType:
            return willHitText ? vec3(0.0) : 2.0*vec3(1, 1, 1);
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

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

vec3 getColor(in MarchResult result)
{
    vec3 color = vec3(0);

    
    vec3 lightPosition = vec3(10, 10, 0);
    vec3 normal = normal(result.position);
    vec3 invLight = normalize(lightPosition - result.position);
    float diffuse = max(0., dot(invLight, normal));
    vec3 ambientColor = getAmbientColor(result.type, result.position, normal);
    color += ambientColor * (0.02 + 0.98*diffuse);
    float k = max(0.0, dot(rayDirection, reflect(invLight, normal)));
    float spec = 1 * pow(k, 30.0);
    color += spec;
    vec3 ao = vec3(float(result.steps) / 600);
    float aof = result.type == boatType ? 0.2 : 0.75;
    if (result.type == invalidType && result.jump == 0) {
        float pitch = asin(result.rayDirection.y);
        float yaw = atan(result.rayDirection.z, result.rayDirection.x);

        vec2 uv;
        uv.x = (yaw + PI) / (2.0 * PI);
        uv.y = (pitch + PI / 2.0) / PI;
        float hf = ojTime < P_0 ? 25 : 7;
        float h = texture(inTexture1, uv * hf).x;
        color = mix(color, color + 11*vec3(clamp(h, 0.0, 1.0)), h);
        return result.scatteredLight + result.transmittance * mix(color, ao, aof);
        } else {
            return result.scatteredLight + result.transmittance *  mix(color, ao, aof);
    }
}

float getFogAmount(in vec3 p)
{
    return 0.003 + 0.001*smoothstep(18, 20, ojTime);
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 pStars = p;
    vec2 iStars = pMod2(pStars.xz, vec2(50, 50));
    pStars.y -= 50 + sin(iStars.x * 10) * 10 + sin(iStars.y * 10) * 10;
    float dStars = length(pStars) - 0.1;

    vec3 pOrig = p;

    vec3 laserFloorP = p.zyx;

    float dm = g_MountainHeight;
    laserFloorP.y +=  dm;
    //laserFloorP.y += sin(p.x);;
    vec3 ufo = ufoPos();
    float laserFloorDis = abs(ufo.x - pOrig.x);
    float dLaserFloor = sdCylinder(laserFloorP, 0.05 + 0.3 * smoothstep(0, 10, laserFloorDis));
    dLaserFloor = max(dLaserFloor,  p.x - ufo.x + 0.5); // cut off in front of UFO
    
    p -= ufo;
    

    float dLaser = sdCylinder(p.xzy, 0.1);
    dLaser = max(dLaser, p.y);

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



    // uniform float C_1_T;
    // uniform float C_6_T;
    // uniform float C_3_T;
    // uniform float C_0_T;
    // uniform float C_2_T;
    // uniform float C_5_T;
    // uniform float C_4_T;

    if (mod(C_4_T, 8.0) == mod(section + 7.0, 8.0) && ojTime > P_25) {
        //tilt = p.x*0.35;
        capsuleStr = 12;
    }

    capsuleStr = max(capsuleStr, 15 - 8*C_5_S);

    p.y -= tilt;
    float dUfoSpin = sdVerticalCapsule(p.yxz - (vec3(0, 0, 0)), 8,  0.01);

    bool showLaser = ojTime < P_25 && ojTime > P_0;

    vec3 color = vec3(0.1, 1, 1);
    vec3 res = color * capsuleStr / (dUfoSpin * dUfoSpin);

    if (showLaser) {
        vec3 laserColor = vec3(1, 0.1, 0.1);
        float laserStr = 50;
        res += laserColor * laserStr / (dLaser * dLaser);

   
        float laserFloorStr = 50 - 50 * C_1_S; 
        if (ojTime > P_1) { 
            res += laserColor * laserFloorStr / (dLaserFloor * dLaserFloor);
        }
    }


    float finalDis = dLaserFloor;
    if (showLaser) {
        //finalDis = min(finalDis, laserFloorDis); // think this one cuses the white AO wall, maybe something wrong with it
        finalDis = min(finalDis, dLaser);
    }
    finalDis = min(finalDis, dUfoSpin);

    return VolumetricResult(finalDis, res * smoothstep(1.0,5.0, ojTime)); 
    //return VolumetricResult(dUfoSpin, res); 
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
    float d = sdPlane(p, vec4(0, 1, 0, 0)) + 0.002*noise_2(5*p.xz + ojTime);
    return d;
}

float mountainH(vec3 p) // just the height
{
    if (ojTime > P_1) { // Shift mountains to something which works better for laser
        p.x += 20;
        p.z += 100;
    }
    
	float h = 4*texture(inTexture0, (p.xz)/90.0).x + 
              200*pow(texture(inTexture0, (p.xz)/1600.0).x, 4);
    
	return - h + 10;
}

float mountain(vec3 p)
{
    float h = g_MountainHeight;
	return p.y + h;
}

float opSubtraction( float d1, float d2 )
{
    return max(-d1,d2);
}

float mountainLaser(vec3 p)
{
    float dMountain = mountain(p);
    vec3 laserFloorP = p.zyx;
    laserFloorP.y += g_MountainHeight - 0.5;
    float dLaserFloor = sdCylinder(laserFloorP, 1.0);

    dLaserFloor = max(dLaserFloor, -p.x);
    dLaserFloor = max(dLaserFloor,  p.x - ufoPos().x + 0.5);

	return opSubtraction(dLaserFloor, dMountain);
}

float opIntersection( float d1, float d2 )
{
    return max(d1,d2);
}

float boat(vec3 p) {
    // return sdBox(p, vec3(2));
    p -= vec3(0.03 * sin(ojTime), 0.06 * sin(ojTime + 3), 0.06 * sin(ojTime + 5));
    p.xz *= rot(PI);
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
    if (p.z < 0.5) {
        dir = -1.0;
    } else {
        dir = 1.0;
    }
    p.y += mod(boatSplitTime * 0.3, 5.0);
    p.z -= dir*5;
    p.zy *= rot(dir*boatSplitTime*0.1);
    p.z += dir*5;

    p.xy *= rot(dir*boatSplitTime*0.3);

     float h = boat(p);
    float d = sdBox(p - vec3(0, 0, dir*4.95), vec3(5));
    return h;
}


float ufo(in vec3 p)
{
    p -= ufoPos();
    float d2 = sdTorus(p - vec3(0, -3, 0), vec2(8.5, 0.5));

    float d1 = length(p) - (1.5 + max(0.5 - C_1_S*3, 0));

    float d3 = sdTorus(p - vec3(0, -0.5, 0), vec2(1.5, 0.5));
#ifdef SPIN_UFO
    p.xz *= rot(iTime * 2.0);
#endif
#ifdef SPIN_UFO_SLOW
    float t = iTime - 4.0;
    t *= smoothstep(4.0, 10.0, iTime);
    t = max(0.0, t);
    p.xz *= rot(t * 2.0);
#endif
    pModPolar(p.xz, 16);
    float d4 = length(p - vec3(8.9, -3, 0)) - 0.3;
    return min(smink(d2, d4, 0.7), smink(d1, d3, 1.5));
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
   DistanceInfo waterInfo = {water(p), waterType};
   DistanceInfo ufoInfo = {ufo(p), ufoType};
   DistanceInfo d = un(waterInfo, un(ufoInfo, mountainDis)); 

    #ifdef SHOW_BOAT
        if (ojTime > P_0 && ojTime < P_1) {
            d = sunk(boatFrontDis, d, 0.15);
        }
    #endif
    
    return d;
}


float borgilaText(vec3 p)
{
    if (p.y < 0.08) {
        return 100;
    }
    float dir = -1.0;
    p.y += mod(boatSplitTime * 0.3, 5.0);
    p.z -= dir*5;
    p.zy *= rot(dir*boatSplitTime*0.1);
    p.z += dir*5;

    p.xy *= rot(dir*boatSplitTime*0.3);

    p -= vec3(0.03 * sin(ojTime), 0.06 * sin(ojTime + 3), 0.06 * sin(ojTime + 5));
    p.xz *= rot(PI);
    p = vec3(-p.z, p.y, p.x);
    p.y -= 1.0;
    p.z -= -0.6;
    p.x -= -4;
    vec2 uv;
    float d =  uvBox(p, vec3(0.6, 0.25, 0.03), uv);
    uv.x *=-1;
    if ( d < 0.001) {
        float s = texture(borgilaTexture, uv).x;
        if (s > 0.2) { // If not on text
            d = 100;
        }
	}
    return d;
}


bool willHitBorgilaText(vec3 rayOrigin, vec3 rayDirection) {
    float t = 0;
    float lastJumpDistance = 10000;
    for (int steps = 0; steps < 20; ++steps) {
        vec3 p = rayOrigin + t * rayDirection;
        float d = borgilaText(p);
        if (d < S_distanceEpsilon) {
            return true;
        }
        t += d;
        if (d > lastJumpDistance) {
            return false;
        }
        lastJumpDistance = d;
    }
    return false;
}


void main()
{
    const float transitionTime = 0.75;
    float a = clamp(ojTime - P_1 + transitionTime, 0, transitionTime) / transitionTime;
    if (a > fragCoord.x) {
        //ojTime += 10;
        P_1 -= transitionTime;
    }

    float b = clamp(ojTime - P_25 + transitionTime, 0, transitionTime) / transitionTime;
    if (b > 1 - fragCoord.x) {
        P_25 -= transitionTime;
    }

    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    float zoom = 1.0;
    if (ojTime < P_0) {
        zoom = 0.8 - 0.5*smoothstep(2, 4, ojTime);
    } else if (ojTime > P_2 &&  ojTime < P_25) {
        zoom = 0.8;
    }
    u *= zoom;
    v *= zoom;

    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    float focus = 0.0;

    

    if (ojTime < P_0) {
        vec3 ufo = ufoPos();
        rayOrigin = vec3(ufo.x - 200, 4, ufo.z);
        vec3 tar = ufo;


        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    
    } else if (ojTime < P_1) {
        rayOrigin = vec3(9.1394, 2.31, -12.4126);
        vec3 tar = vec3(-2, 1, 1);

        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    } else if (ojTime < P_2) {
        rayOrigin = vec3(15, 8.28, 20);
        vec3 tar = rayOrigin + vec3(0.5, 0, -0.5);

        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    } else if (ojTime < P_25) {
        float spin = (ojTime - P_2) * 0.2;
        float d = 60;
        rayOrigin = ufoPos() + vec3(d*sin(spin), 10, d*cos(spin));
        vec3 tar = ufoPos();

        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    } else if (ojTime < P_4) { // part 3 and 4 in same case
        S_volumetricDistanceMultiplier = 0.2; // lower is needed here to avoid artifacts


        vec3 ufo = ufoPos();
        rayOrigin = vec3(ufo.x - 30, 15, ufo.z);
        vec3 tar = ufo;//rayOrigin + vec3(1.0, -0.4, 0);

        //vec3 tar = ufo + vec3(1.0, -0.4, 0);

        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    }

    firstRayDirection = rayDirection;
    willHitText = willHitBorgilaText(rayOrigin, rayDirection);
    FullMarchResult res = march2(rayOrigin, rayDirection);
    vec3 color = res.col;

    // fade to black
    float transitionTimeFadeToBlack = 1.5;
    float fade = clamp(ojTime - P_4 + transitionTimeFadeToBlack, 0, transitionTimeFadeToBlack) / transitionTimeFadeToBlack;
    if (ojTime < P_4) {
        color = mix(color, vec3(0), fade);
    }

     color /= (color + vec3(1.0));

     if (ojTime < P_0) {
        focus =  1.0 - smoothstep(3, 4, ojTime);
     } else if (ojTime > P_25 ) { //for scene 3 & 4
        vec3 ufo = ufoPos();
         focus = abs(length(res.firstJumpPos - ufo)) * 0.005;// + 0.01;
        
        float t4 = max(0, ojTime - P_3);
        focus = mix(focus, 1 - smoothstep(0, 1, t4), t4); // make clearer as ufo ascends

     } 
    fragColor = vec4(pow(color, vec3(0.5)), clamp(focus, 0.001, 2.0));
}

)""

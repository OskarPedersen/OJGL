R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
float S_volumetricDistanceMultiplier = 0.5;
const int S_reflectionJumps = 3;

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

uniform float scenePart;

const int ufoType = 5;
const int mountainType = 6;
const int runwayType = 7;
const int hangarType = 8;
const int doorsType = 9;
const int boatType = 10;

vec3 cameraPosition;
vec3 rayDirection;
vec3 firstRayDirection;

float hangarBox(in vec3 p);
float runwayBox(in vec3 p);

float ufoSpeed = 10.0;

const float ufoPosD1 = 3;
const float ufoPosD2 = 4;
const float ufoPosD3 = 5;

const float doorOpenTimePart2 = 2;
const float waitForLaserTime = 2;
const float laserPeakTime = 2.5;

const float camera1 = ufoPosD1 + ufoPosD2 - 1;
const float camera2 = camera1 + ufoPosD3 - 2;

bool ufoVisible() {
    return scenePart == 1.0 || (scenePart == 2.0 && iTime < (laserPeakTime + doorOpenTimePart2 + waitForLaserTime));
}

vec3 ufoRot(in vec3 p) {
    if (scenePart == 2.0) {
        return p;
    }
    float rotEnd = ufoPosD1 + 0.5;
    if (iTime < rotEnd) {
        float s = 1.0 - smoothstep(rotEnd - 1.0, rotEnd, iTime);
        p.yz *= rot(sin(iTime * 1.5) * 0.1 * s);

        p.xy *= rot(0.3 * s);
    }
    return p;
}

vec3 ufoPos()
{
    vec3 endPos = vec3(40, 0, 30);

    if (scenePart == 2.0) {
        return endPos;
    }

    float t = iTime;


    if (t < ufoPosD1) {
        return mix(vec3(-150, 30, 0), vec3(-40, 2, 0), (t) / ufoPosD1);
    } else if (t < ufoPosD1 + ufoPosD2) {
     return mix(vec3(-40, 2, 0), vec3(40, 1, 0), (t - ufoPosD1) / ufoPosD2);
    } else if (t < ufoPosD1 + ufoPosD2 + ufoPosD3) {
     return mix(vec3(40, 1, 0), endPos, smoothstep(0, 1, (t - ufoPosD1 - ufoPosD2) / ufoPosD3));
    }


    return endPos;
}


float opIntersection( float d1, float d2 )
{
    return max(d1,d2);
}

vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case ufoType:
            return vec3(1, 0, 1);
        case mountainType:
            return 2*vec3(1, 0.3, 0.1);
        case runwayType:
            return vec3(1, 0.9, 0.8);
        case hangarType:
            return mod(pos.z, 3.0) > 1.5 ? vec3(1.0) : vec3(0.5);
        case doorsType:
            return vec3(1, 0.9, 0.4);
        case boatType:
            return 20.0*vec3(1, 1, 1);
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

    
    vec3 lightPosition = vec3(-100, 50, -10);
    if (iTime < camera1 && scenePart == 1.0) {
        lightPosition = vec3(100, 50, -10);
    } else if (iTime > camera2 && scenePart == 1.0) {
        lightPosition = vec3(10, -5, -10);
    }
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
    return 0.005;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 pOrig = p;

    vec3 laserFloorP = p.zyx;

    vec3 ufo = ufoPos();

    p -= ufo;

    float finalDis = 99999;
    vec3 res = vec3(0);

    if (ufoVisible()) {
        p = ufoRot(p);

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
        res = color * capsuleStr / (dUfoSpin * dUfoSpin);
    
        finalDis = dUfoSpin;

    }

    vec3 runwayColor = vec3(1.0, 0.1, 0.7);
    p = pOrig;
    p.y -= -3.5;
    p.z = abs(p.z);
    p.z -= 12;

   pMod1(p.x, 10);
    float dRunway = sdBox(p, vec3(0.1, 0.3, 0.1));
    dRunway = opIntersection(dRunway, runwayBox(pOrig));
    float strRunway = 100;


    res += runwayColor * strRunway / (dRunway * dRunway);
    finalDis = min(finalDis, dRunway);
 
    if (scenePart == 2.0) {
        p = pOrig;
        p = p.xzy;
        p -= vec3(39.25, 15, 1.25);

        float tt = iTime - doorOpenTimePart2 - waitForLaserTime;
        float t = min(tt, laserPeakTime*2.0-tt);

        float len = 2 + min(t, 2.0) * 10;

        float dLaser = sdVerticalCapsule(p - (vec3(0, 0, 0)), len,  0.1);
        float strLaser = 100 + max(0, t - 1.5) * 100000.0;
        vec3 laserColor = vec3(1.0, 0.05, 0.05);

        res += laserColor * strLaser / (dLaser * dLaser);
        finalDis = min(finalDis, dLaser);
    }


    return VolumetricResult(finalDis, res); 
}

float getReflectiveIndex(int type) {
    switch (type) {
        case ufoType:
            return 1.0;
        case mountainType:
            return 0;
        case runwayType:
            return 0.1;
        case doorsType:
            return 0.1;
        case hangarType:
            return 0;
        case boatType:
            return 0.1;
        default:
           return 0.0;
    }
}


float opSubtraction( float d1, float d2 )
{
    return max(-d1,d2);
}



float mountainH(vec3 p)
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
    float d = p.y + h;

    float inside = hangarBox(p);
    d = opSubtraction(inside, d);

	return d;
}



float ufo(in vec3 p)
{
    p -= ufoPos();

    p = ufoRot(p);

    float d2 = sdTorus(p - vec3(0, -3, 0), vec2(8.5, 0.5));

    float d1 = length(p) - (2.0 + max(0.5 - C_1_S*3, 0));
    return min(d1, d2);
}

DistanceInfo sunk(DistanceInfo a, DistanceInfo b, float k) {
    DistanceInfo res = a.distance < b.distance ? a : b;
    res.distance = smink(a.distance, b.distance, k);
    return res;
}

vec3 runwayPos = vec3(0, -5, 0);
vec3 runwaySize = vec3(65, 1, 15);


float runwayBox(in vec3 p) 
{
    vec3 b = runwaySize;
    b.y = 999999;
    p -= runwayPos;
    float d = sdBox(p, b);
    return d;
}

float runway(in vec3 p) 
{
    vec3 b = runwaySize;
    p -= runwayPos;
    p.y += 0.3*texture(inTexture0, (p.xz)/200.0).x;
    float d = sdBox(p, b);
    return d;
}

vec3 hangarPos = vec3(40, -5, 33);

float hangarBox(in vec3 p) {
   p -= hangarPos;
    float w = p.y;

   vec3 b = vec3(15 - w * 0.6 + 10, 15, 18);

  //p.x -= 0.5*texture(inTexture0, (p.yz)/200.0).x;
  //p.y -= 0.5*texture(inTexture0, (p.xz)/200.0).x;
  //p.z -= 0.5*texture(inTexture0, (p.xy)/200.0).x;

  return sdBox(p, b);
}

float hangar(in vec3 p) 
{
    float d = hangarBox(p);

    p -= hangarPos;
    p.y -= 7;

    p.z -= -10;
    
   float s = 0.1;
   float r = 20.0;
   //p.x -= s*texture(inTexture0, (p.yz)/r).x;
   p.y -= s*texture(inTexture0, (p.xz)/r).x;
   //p.y -= (sin(p.x) + sin(p.z)) * 0.1;
   //p.z -= s*texture(inTexture0, (p.xy)/r).x;


    float inside = sdBox(p, vec3(13, 6, 16));
    d = opSubtraction(inside, d);
    return d;
}

float doors(in vec3 p) 
{
    float t = min(iTime,  ufoPosD1 + ufoPosD2 + ufoPosD3);

    float open = 1.0;
    if (t > ufoPosD1 + ufoPosD2) {
        float tt = t - (ufoPosD1 + ufoPosD2);
        open = max(0.0, max(1.0 - tt, tt - ufoPosD3 + 1));
    }

    if (scenePart == 2.0) {
        float t = max(0, iTime - doorOpenTimePart2);
        open = max(0.1, 1 - t);

    }

    float w = 6.5;

    vec3 b = vec3(w*open, 13, 0.5);
    //p.x = abs(p.x + w) - w;
     p.z -= 0.5*texture(inTexture0, (p.xy)/200.0).x;

    float d1 = sdBox(p - vec3(40 + w*2 - w * open, -5, 17), b);
    float d2 = sdBox(p - vec3(40 - w*2 + w * open, -5, 17), b);
    //float d2 = sdBox(p - vec3(40 - w * open - w, -5, 17), b);

    return min(d1, d2);

}

float boat(vec3 p) {
  
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

    
    vec3 p5 = p;
    p5.z = abs(p5.z);
    p5.z -= 4.6;
    p5.y -= 3.2;
    p5.zy *= rot(-1.1);
    float line2 = sdBox(p5, vec3(0.01, 0.01, 2.05));


    float h = min(line2, min(mast, min(windows, hull)));

    return h;
}

float boatSplit(vec3 p, float dir)
{
    p.xz = p.zx;


    p -= vec3(10, 0, 33);

    p *= 0.4;

    float h = boat(p);

    vec3 cannonPos = vec3(1.5, 0.5, 2.5);
    float cannonOuter = sdCappedCylinder(p.yxz - cannonPos.yxz, vec2(0.4, 1.0));
    float cannonInner = sdCappedCylinder(p.yxz - cannonPos.yxz, vec2(0.2, 100));
    float cannon = opSubtraction(cannonInner, cannonOuter);
    h = min(h, cannon);

    float d = sdBox(p - vec3(0, 0, dir*4.95), vec3(5));
    return max(d, h);
}


DistanceInfo map(in vec3 p)
{
   DistanceInfo ufoInfo = {ufo(p), ufoType};
   DistanceInfo mountainInfo = {mountain(p), mountainType};

   DistanceInfo runwayInfo = {runway(p), runwayType};
   DistanceInfo hangarInfo = {hangar(p), hangarType};
   DistanceInfo doorsInfo = {doors(p), doorsType};

   DistanceInfo di = un(un(runwayInfo, un(hangarInfo, doorsInfo)), mountainInfo);
   if (ufoVisible()) {
        di = un(di, ufoInfo);
   }
   if(scenePart == 2.0) {
       DistanceInfo boatFrontDis = { boatSplit(p, 1.0), boatType };
        di = un(di, boatFrontDis);
   }

   return di;
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

    if (scenePart == 1.0) {
         if (iTime > camera1) {
            zoom = 1.5;
         }
    } else { // part 2
        zoom = 1.6; //2.0;
    }
    
    u *= zoom;
    v *= zoom;

    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    float focus = 0.0;

    if (scenePart == 1.0) {
        if (iTime < camera1) {
            vec3 ufo = ufoPos();
            rayOrigin = ufo + vec3(-15, 8, 0);
            vec3 tar = rayOrigin + vec3(10, -3 ,0 );
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);
        
            rayDirection = normalize(dir + right*u + up*v);
    
        } else if (iTime < camera2) {
            vec3 ufo = ufoPos();
            rayOrigin = vec3(62.094, 16.86, -25.4996);
            vec3 tar = ufo;
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);

             rayDirection = normalize(dir + right*u + up*v);
        } else {
            vec3 ufo = ufoPos();
            rayOrigin = vec3(40, 5, 38);
            vec3 tar = rayOrigin + vec3(0, -0.2, -1);
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);

             rayDirection = normalize(dir + right*u + up*v);
        }
    } else { // part 2
        vec3 ufo = ufoPos();
        rayOrigin = vec3(40, 5, 38);
        vec3 tar = rayOrigin + vec3(0, -0.2, -1);
        
        vec3 dir = normalize(tar - rayOrigin);
	    vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	    vec3 up = cross(dir, right);

        rayDirection = normalize(dir + right*u + up*v);
    }
  
    

    firstRayDirection = rayDirection;

    FullMarchResult res = march2(rayOrigin, rayDirection);
    vec3 color = res.col;

    if (scenePart == 2.0) {
        const float fadeOutTime = doorOpenTimePart2 + waitForLaserTime + laserPeakTime * 2.0 + 1;
        if (iTime > fadeOutTime) {
            float t = iTime - fadeOutTime;
            color = mix(color, vec3(0), min(1, t)); 
        }

    }

     color /= (color + vec3(1.0));

     if (scenePart == 2.0) {
        //focus = 0.1;
     }
    fragColor = vec4(pow(color, vec3(0.5)), clamp(focus, 0.001, 2.0));
}

)""

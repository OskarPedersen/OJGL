R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.9;
const float S_minVolumetricJumpDistance = 0.005;
float S_volumetricDistanceMultiplier = 0.8;
const int S_reflectionJumps = 2;
float g_MountainHeight = 0.0;

#define S_VOLUMETRIC 1
#define S_REFLECTIONS 1
#define S_REFRACTIONS 0

#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/utils.fs"
#include "edison2025/ufo_landing_utils.fs"
#include "edison2025/ufo_raymarch_utils.fs"


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
            return vec3(0.0);
        case doorsType:
            return 0*vec3(1, 0.9, 0.4);
        case boatType:
            return willHitText ? vec3(0.0) : 15.0*vec3(1, 1, 1);
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
    } else if (scenePart == 2.0) {
        lightPosition = ufoP() + vec3(-50, 50, 40);
    }
    vec3 normal = normal(result.position);
    vec3 invLight = normalize(lightPosition - result.position);
    float diffuse = max(0., dot(invLight, normal));
    vec3 ambientColor = 0.1*getAmbientColor(result.type, result.position, normal);
    color += ambientColor * (0.02 + 0.98*diffuse);
    float k = max(0.0, dot(rayDirection, reflect(invLight, normal)));
    float spec = 1 * pow(k, 30.0);
    color += spec;

    vec3 ao = vec3(float(result.steps) / 600);
    if (result.type == invalidType) {
        float pitch = asin(rayDirection.y);
        float yaw = atan(rayDirection.z, rayDirection.x);

        vec2 uv;
        uv.x = (yaw + PI) / (2.0 * PI);
        uv.y = (pitch + PI / 2.0) / PI;
        float h = texture(inTexture1, uv * 5).x + 0.1*hash11(fragCoord.x+cos(fragCoord.y));
        color = result.scatteredLight;
        color = mix(color, color + 0.3*vec3(clamp(h, 0.0, 1.0)), h);
        return color;
    } else {
        float shadow = shadowFunction(result.position, result.type);
        return result.scatteredLight + result.transmittance *  mix(color * shadow, ao, 0.75);
    }

}

float sdc( vec3 p, vec3 a, vec3 b, float r )
{
  vec3 pa = p - a, ba = b - a;
  float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
  return length( pa - ba*h ) - r;
}

float getFogAmount(in vec3 p)
{
    return 0.005;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 po = p;

    vec3 laserFloorP = p.zyx;

    vec3 ufo = ufoP();

    p -= ufo;

    float fd = 99999;
    vec3 res = vec3(0);

    if (ufoV()) {
        p = ufoR(p);

        float sec = pModPolar(p.xz, 16);
    
        float tilt = -p.x*0.35;
    
        float cs = 5;
        if (mod(sec, 4.0) == 0.0) {
            cs = 5.0 + max(0, 10 - C_7_S_0 * 100);
            tilt *= min(1, C_7_S_0 * 3.0);

        } else if (mod(sec, 4.0) == 1.0) {
            cs = 5.0 + max(0, 10 - C_7_S_1 * 100);
             tilt *= min(1, C_7_S_1 * 3.0);

        } else if (mod(sec, 4.0) == 2.0) {
            cs = 5.0 + max(0, 10 - C_7_S_2 * 100);
             tilt *= min(1, C_7_S_2 * 3.0);

        } else if (mod(sec, 4.0) == 3.0) {
            cs = 5.0 + max(0, 10 - C_7_S_3 * 100);
             tilt *= min(1, C_7_S_3 * 3.0);

        }


        p.y -= tilt;
        float dUfoSpin = sdVerticalCapsule(p.yxz - (vec3(0, 0, 0)), 8,  0.01);
    
        vec3 color = vec3(0.1, 1, 1);
        res = color * cs / (dUfoSpin * dUfoSpin);
    
        fd = dUfoSpin;

    }

    vec3 rc = vec3(1.0, 0.1, 0.2);
    p = po;
    p.y -= -3.8;
    p.z = abs(p.z);
    p.z -= 12;

   pMod1(p.x, 10);
   float h = 0.3 + C_1_S;
    float dRunway = sdBox(p, vec3(0.1, h, 0.1));
    dRunway = opIntersection(dRunway, runwayBox(po));
    float strRunway = 100 - 120*C_1_S;
    strRunway = max(strRunway, 0);

    res += rc * strRunway / (dRunway * dRunway);
    fd = min(fd, dRunway);
    
    if (scenePart == 1.0 && iTime < 6) {
       p = po;
       p.y -= -3.8;


       const float sections = 15;
       float section = pMod1(p.x, sections);
       if (floor(section + sections / 2) == floor(mod(iTime * 10, 15))) {
           float dLanding1 = sdc(p, vec3(0, 0, 0), vec3(-3, 0, 1), 0.2);
           float dLanding2 = sdc(p, vec3(0, 0, 0), vec3(-3, 0, -1), 0.2);
            float dLanding = min(dLanding1, dLanding2);

           dLanding = opIntersection(dLanding, runwayBox(po));
           float strLanding = 10 ;
           strLanding = max(strLanding, 0);

           res += rc * strLanding / (dLanding * dLanding);
           fd = min(fd, dLanding);
       }
    }


    if (scenePart == 2.0) {
      
        {
            vec3 pEngine = po;

            pEngine.xyz = pEngine.zyx;

            pEngine -= bp() + vec3(1, 0, -2);
            const float engineW = 0.7;
            pEngine.z -= engineW;

            
            float w = 0.4 + sin(p.z* 1000) * 0.1;
            
            w -= 0.1*sin(po.z * 1000.0 + iTime * 2000);

            float depth = 1.2 + 0.8*max(0.5 - C_3_S*3, 0);

            float dEngine1 = sdCappedCylinder(pEngine.xzy, vec2(w, depth));
            pEngine.x += engineW * 2.0;
            float dEngine2 = sdCappedCylinder(pEngine.xzy, vec2(w, depth));
            float dEngine = min(dEngine1, dEngine2);

             float engineStr = 100 + sin(iTime * 30) * 10;

            vec3 engineColor = mix(vec3(1.0, 0.1, 0.01), vec3(1.0, 0.0, 0.01), mod(p.z, 1.0));//vec3(1.0);;
            res += engineColor * engineStr / (dEngine * dEngine);

            fd = min(fd, dEngine);
        }


        if (iTime > doorOpenTimePart2 + waitForLaserTime) {
            p = po;
            p = p.xzy;

        

            float t2 = iTime * 3.0;
            //p -= vec3(0.05 * sin(t2), 0.1 * sin(t2 + 3), 0.1 * sin(t2 + 5)).zyx;
            p -= vec3(0.05 * sin(t2), 0.1 * sin(t2 + 3), 0.1 * sin(t2 + 5)).zxy;


            p -= vec3(40, 15, 1.25);

            float tt = iTime - doorOpenTimePart2 - waitForLaserTime - part2flybyEndTime;
            float t = min(tt, laserPeakTime*2.0-tt);

            float len = 2 + min(t, 2.0) * 10;

            float dLaser = sdVerticalCapsule(p - (vec3(0, 0, 0)), len,  0.1);
            float strLaser = 100 + max(0, tt - 1.5) * 100000.0;
            vec3 laserColor = vec3(1.0, 0.05, 0.05);

            res += laserColor * strLaser / (dLaser * dLaser);
            fd = min(fd, dLaser);
        }
    }


    return VolumetricResult(fd, res); 
}

float getReflectiveIndex(int type) {
    switch (type) {
        case ufoType:
            return 1.0;
        case mountainType:
            return 0;
        case runwayType:
            return 0;
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
    p -= ufoP();
    p = ufoR(p);
    float d2 = sdTorus(p - vec3(0, -3, 0), vec2(8.5, 0.5));

    float d1 = length(p) - (1.5 + max(0.5 - C_1_S*3, 0));

    float d3 = sdTorus(p - vec3(0, -0.5, 0), vec2(1.5, 0.5));
    if (scenePart == 1.0) {
        p.xz *= rot(iTime * 2.0);
    }
    pModPolar(p.xz, 16);
    float d4 = length(p - vec3(8.9, -3, 0)) - 0.3;
    return min(smink(d2, d4, 0.7), smink(d1, d3, 1.5));
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
    float d = sdBox(p, b);
    return d;
}

vec3 hangarPos = vec3(40, -5, 33);

float hangarBox(in vec3 p) {
   p -= hangarPos;
    float w = p.y;
   vec3 b = vec3(15 - w * 0.6 + 10, 15, 18);
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
    float inside = sdRoundBox(p, vec3(13, 6, 16), 0.1);
    d = opSubtraction(inside, d);
    return d;
}

float doors(in vec3 p) 
{
    float t = min(iTime,  ufoPD1 + ufoPD2 + ufoPD3);

    float open = 1.0;
    if (t > ufoPD1 + ufoPD2) {
        float tt = t - (ufoPD1 + ufoPD2);
        open = max(0.0, max(1.0 - tt, tt - ufoPD3 + 1));
    }

    if (scenePart == 2.0) {
        float t = max(0, iTime - doorOpenTimePart2);
        open = max(0.1, 1 - t);

    }

    float w = 6.5;
    vec3 b = vec3(w*open, 13, 0.5);
     p.z -= 0.2*texture(inTexture0, (p.xy)/200.0).x;
    float d1 = sdBox(p - vec3(40 + w*2 - w * open, -5, 17), b);
    float d2 = sdBox(p - vec3(40 - w*2 + w * open, -5, 17), b);

    return min(d1, d2);

}

float boat(vec3 p) {
  
    float ffz = p.z > 0.0 ? -4.0 : -7.0;
    float fz = 1.7 - 0.7 * smoothstep(ffz, 2.0, p.y);
    float fx = 0.971*smoothstep(3, 7, abs(p.z));
    float fx2 = 1*smoothstep(-3.0, 2.0, p.y);
    float fy = 0.5*smoothstep(3, 7, p.z);
    
    vec3 p1 = p;
    p1 -= vec3(0, 0.4, 0);
    float fx3 = 1.7*(1 - smoothstep(-2.0, 0.5, p.y));
    p1 -= vec3(0, 0.4, 0);
    float ff = 0.3;
    p1.y += ff;

    float hull = sdBox(p1, vec3(2 - fx - fx2 - fx3, 1.0 + fy + ff, 7 / fz));
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
    p.xyz = p.zyx;
    p -= bp();
    
    p *= 0.4;
    float h = boat(p);
    vec3 cannonPos = vec3(1.5, 0.5, 2.5);
    float cannonOuter = sdCappedCylinder(p.yxz - cannonPos.yxz, vec2(0.3, 1.0));
    float cannonInner = sdCappedCylinder(p.yxz - cannonPos.yxz, vec2(0.15, 100));
    float cannon = opSubtraction(cannonInner, cannonOuter);
    h = smink(h, cannon, 0.5);

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
   if (ufoV()) {
        di = un(di, ufoInfo);
   }
   if(scenePart == 2.0) {
       DistanceInfo boatFrontDis = { boatSplit(p, 1.0), boatType };
        di = un(di, boatFrontDis);
   }

   return di;
}

float borgilaText(vec3 p)
{
    float f = 1/0.4;
    p.xz = p.zx;
    p -= bp();
    p.y -= 1.0 * f;
    p.z -= 4.4 * f;
    p.x -= 0.5 * f;
    p = vec3(-p.z, p.y, p.x);
    vec2 uv;
    float d =  uvBox(p, vec3(0.6*f, 0.25*f, 0.03), uv);
    uv.x *=-1;
    if ( d < 0.001) {
        float s = texture(borgilaTexture, uv).x;
        if (s > 0.1) {
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


    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    float zoom = 1.0;

    if (scenePart == 1.0) {
         if (iTime > camera1) {
            zoom = 1.5;
         }
    } else {
        zoom = 1.6;
    }
    
    u *= zoom;
    v *= zoom;

    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    float focus = 0.0;

    if (scenePart == 1.0) {
        if (iTime < camera1) {
            vec3 ufo = ufoP();
            rayOrigin = ufo + vec3(-15, 8, 0);
            vec3 tar = rayOrigin + vec3(10, -3 ,0 );
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);
        
            rayDirection = normalize(dir + right*u + up*v);
    
        } else if (iTime < camera2) {
            vec3 ufo = ufoP();
            rayOrigin = vec3(62.094, 16.86, -25.4996);
            vec3 tar = ufo;
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);

             rayDirection = normalize(dir + right*u + up*v);
        } else {
            vec3 ufo = ufoP();
            rayOrigin = vec3(40, 5, 38);
            vec3 tar = rayOrigin + vec3(0, -0.2, -1);
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);

             rayDirection = normalize(dir + right*u + up*v);
        }
    } else {
        if (iTime < part2flybyEndTime) {
            vec3 boat = bp();
            rayOrigin = boat + vec3(20, 20, 20);
            vec3 tar = boat;
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);

            rayDirection = normalize(dir + right*u + up*v);
        } else {
            rayOrigin = vec3(40, 5, 38);
            vec3 tar = rayOrigin + vec3(0, -0.2, -1);
        
            vec3 dir = normalize(tar - rayOrigin);
	        vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	        vec3 up = cross(dir, right);

            rayDirection = normalize(dir + right*u + up*v);
        }
    }
    firstRayDirection = rayDirection;

    willHitText = willHitBorgilaText(rayOrigin, rayDirection);
    FullMarchResult res = march2(rayOrigin, rayDirection);
    vec3 color = res.col;

     color /= (color + vec3(1.0));
    fragColor = vec4(pow(color, vec3(0.5)), clamp(focus, 0.001, 2.0));

    if(scenePart == 1.0) {
        fragColor.xyz *= smoothstep(0.0, 0.5, iTime);
        fragColor.xyz *= 1.0 - smoothstep(13.0, 13.5, iTime);
    }
}


)""

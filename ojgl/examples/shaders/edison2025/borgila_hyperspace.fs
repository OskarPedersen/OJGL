R""(

const float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 500.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
float S_volumetricDistanceMultiplier = 0.5;
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
uniform sampler2D borgilaTexture;

uniform float C_1_S; // bass
uniform float C_3_S; // "vocals"
uniform float C_7_S; // "synth"

uniform float C_7_S_0;
uniform float C_7_S_1;
uniform float C_7_S_2;
uniform float C_7_S_3;

uniform float C_7_T; // "synth"

const int boatType = 6;

vec3 cameraPosition;
vec3 rayDirection;
vec3 firstRayDirection;
bool willHitText = false;


float ufoSpeed = 10.0;

vec3 ufoPos()
{
    return vec3(0);
}


vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case boatType:
            return willHitText ? vec3(0.0) : 6*vec3(1, 1, 1);
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

    vec3 lightPosition = vec3(-40, 30, -10);
    vec3 normal = normal(result.position);
    vec3 invLight = normalize(lightPosition - result.position);
    float diffuse = max(0., dot(invLight, normal));
    vec3 ambientColor = getAmbientColor(result. type, result.position, normal);
    color += ambientColor * (0.02 + 0.98*diffuse);
    float k = max(0.0, dot(rayDirection, reflect(invLight, normal)));
    float spec = 1 * pow(k, 30.0);
    color += spec;
    vec3 ao = vec3(float(result.steps) / 200);
    if (result.type == invalidType) {
        return result.scatteredLight;
    } else {
        return result.scatteredLight + result.transmittance *  mix(color, ao, 0.75);
    }

}

float getFogAmount(in vec3 p)
{
    return 0.02;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 pOrig = p;

    vec3 res = vec3(0);


    float dHyperSum = 999999;


 { // hyper 1

       vec3 pHyper = pOrig;
        pHyper.y -= 1;
       pHyper.yz *= rot(pHyper.x * 0.01);
       float iHyper = pModPolar(pHyper.yz, 16);
       pHyper -= vec3(0, 20, sin(pOrig.x + iTime * 60)*5);
       float dHyper = sdCylinder(pHyper.zyx, 0.0);

       float hyperStr = (4 + 3.5*sin(iTime * 10))*0.2;
       vec3 hyperColor = 0.3*vec3(0.1, 0.3, 1.0);
       res += hyperColor * hyperStr / (dHyper * dHyper);

       dHyperSum = min(dHyperSum, dHyper);
   }


   { // hyper 2
        vec3 pHyper = pOrig;
        pHyper.y -= 1;
       pHyper.yz *= rot(pHyper.x * 0.001);
       float iHyper = pModPolar(pHyper.yz, 16);
       pHyper -= vec3(0, 6 + sin(pOrig.x * 0.03 + iTime), -sin(pOrig.x + iTime * 60)*1.1);
       float dHyper = sdCylinder(pHyper.zyx, 0.0);
       //dHyper -= texture(inTexture0, (pOrig.yz)/90.0).x;
       dHyper = max(0.01, dHyper);

       float hyperStr = 0.05;
       vec3 hyperColor = 1.5*vec3(0.2, 0.3, 1.0);
       res += hyperColor * hyperStr / (dHyper);

       dHyperSum = min(dHyperSum, dHyper);
   }

    { // hyper 3

        vec3 pHyper = pOrig;
         pHyper.y -= 1;
        pHyper.yz *= rot(pHyper.x * 0.02);
        float iHyper = pModPolar(pHyper.yz, 16);
        pHyper -= vec3(0, 100, sin(pOrig.x - iTime * 10)*5);
        float dHyper = sdCylinder(pHyper.zyx, 0.0);

        float hyperStr = 0.5;
        vec3 hyperColor = 0.1*vec3(0.6, 0.3, 1.0);
        res += hyperColor * hyperStr / (dHyper * dHyper);

        dHyperSum = min(dHyperSum, dHyper);
    }


    { // Borgila engine
        // float sdCappedCylinder(vec3 p, vec2 h);
        vec3 pEngine = pOrig;

        pEngine = pEngine.yxz;
        pEngine.y -= -36;
        pEngine.x -= 2;
        //pEngine.z = abs(pEngine.z);
        const float engineW = 0.7;
        pEngine.z -= engineW;

        //float w = 0.2;
        float tweak = 0.1 - 0.04*smoothstep(0, 5, iTime);
        float w = 0.12 + sin(p.x* 1000) * tweak;
        //w -= (p.x + 36) * 0.08;
        w -= 0.05*sin(pOrig.z * 1000.0 + iTime * 2000) ;

        float depth = 0.8 + 0.8*max(0.5 - C_3_S*3, 0);
        float dEngine1 = sdCappedCylinder(pEngine, vec2(w, depth));
        pEngine.z += engineW * 2.0;
        float dEngine2 = sdCappedCylinder(pEngine, vec2(w, depth));
        float dEngine = min(dEngine1, dEngine2);

         float engineStr = 10 + sin(iTime * 30) * 1; //1;

        vec3 engineColor = mix(vec3(1.0, 0.1, 0.01), vec3(1.0, 0.0, 0.01), mod(p.z, 1.0));//vec3(1.0);;
        res += engineColor * engineStr / (dEngine * dEngine * dEngine);

        dHyperSum = min(dHyperSum, dEngine);
    }


    float finalDis = dHyperSum;


    return VolumetricResult(finalDis, res);
}

float getReflectiveIndex(int type) {
    switch (type) {
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


float opIntersection( float d1, float d2 )
{
    return max(d1,d2);
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

float boat(vec3 p) {
    p -= vec3(0.03 * sin(iTime), 0.06 * sin(iTime + 3), 0.06 * sin(iTime + 5));

    p.xz *= rot(0.1*cos(1.0*iTime));
    p.xy *= rot(0.1*sin(0.45*iTime));
    float ffz = p.z > 0.0 ? -4.0 : -7.0;
    float fz = 1.7 - 0.7 * smoothstep(ffz, 2.0, p.y);
    float fx = 0.971*smoothstep(3, 7, abs(p.z));
    float fx2 = 1*smoothstep(-3.0, 2.0, p.y);
    float fy = 0.5*smoothstep(3, 7, p.z);
    float fx3 = 1.7*(1 - smoothstep(-2.0, 0.5, p.y));
    vec3 p1 = p;
    p1 -= vec3(0, 0.4, 0);
    float ff = 0.3;
    p1.y += ff;
    float hull = sdBox(p1, vec3(2 - fx - fx2 - fx3, 1.0 + ff + fy, 7 / fz));

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


    //vec3 p4 = p;
    //p4.y -= 4.4;
    //p4.y -= 0.9*smoothstep(0, 5, abs(p.z));
    //p4.y -= -(3.0-abs(p.z*0.8)); *boatSplitTime*4.0; // make line fall down
    //float line = sdBox(p4, vec3(0.01, 0.01, 3.6));

    vec3 p5 = p;
    p5.z = abs(p5.z);
    p5.z -= 4.6;
    p5.y -= 3.2;
    p5.zy *= rot(-1.1);
    float line2 = sdBox(p5, vec3(0.01, 0.01, 2.05));

    //line = min(line, line2);

    float h = min(line2, min(mast, min(windows, hull)));
    return h;
}

float boatSplit(vec3 p, float dir)
{
   // p.y += mod(boatSplitTime * 0.3, 5.0);
    //p.z -= dir*5;
    //p.zy *= rot(dir*boatSplitTime*0.1);
    //p.z += dir*5;

    //p.xy *= rot(dir*boatSplitTime*0.3);

    p.xz = p.zx;

    p.z -= -35;
    p.y -= 2;

   float h = boat(p);

    vec3 p2 = p;
    // p2.yz *= rot(-0.3);
    float d = sdBox(p2 - vec3(0, 0, dir*4.95), vec3(5));
    d = max(d, h);

    vec3 p6 = p;
    p6.x = abs(p6.x);
    p6.z -= -0.3;
    p6.y -= 0.0;
    p6.x -= 0.7;
    float engine = sdSphere(p6, 0.25);
    return min(d, engine);
}

DistanceInfo map(in vec3 p)
{
    DistanceInfo boatFrontDis = { boatSplit(p, 1.0), boatType};
   return boatFrontDis;
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
            lightIntegrated = max(vec3(0), lightIntegrated); // To fix the small black squares that could appear sometimes with bright volumetric light
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


float uvBox(vec3 p, vec3 b, inout vec2 uv)
{
    vec3 d = abs(p) - b;
    float dis = length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);

    uv = p.xy / (b.xy * 2) - 0.5;
    uv.x*=-1;
    return dis;
}

float borgilaText(vec3 p)
{
    
    p.xz = p.zx;
    
    p.z -= -35;
    p.y -= 2;
    // p.xz *= rot(0.1*cos(1.0*iTime));
    // p.xy *= rot(0.1*sin(0.45*iTime));
    p -= vec3(0.03 * sin(iTime), 0.06 * sin(iTime + 3), 0.06 * sin(iTime + 5));
    p.xz *= rot(0.1*cos(1.0*iTime));
    p.xy *= rot(0.1*sin(0.45*iTime));

    p = vec3(-p.z, p.y, p.x);
    
    p.y -= 1.0;
    p.z -= -1;
    p.x -= -4;
    
    
    // return sdSphere(p, 0.5);
    vec2 uv;
    float d =  uvBox(p, vec3(0.6, 0.25, 0.03), uv);
    // uv.x *=-1;
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

    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    float zoom = 1.0;

    u *= zoom;
    v *= zoom;

    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.5, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    float focus = 0.0;

    vec3 ufo = ufoPos();
    rayOrigin = vec3(-50, 7 + 0.1*cos(3*iTime + sin(iTime)) +3 -5*smoothstep(0, 5, iTime), -15*smoothstep(0, 8, iTime));
    vec3 tar = vec3(-30, 4, 0);


    vec3 dir = normalize(tar - rayOrigin);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	vec3 up = cross(dir, right);

    rayDirection = normalize(dir + right*u + up*v);


    firstRayDirection = rayDirection;
    //vec3 color = march(rayOrigin, rayDirection);
    willHitText = willHitBorgilaText(rayOrigin, rayDirection);
    FullMarchResult res = march2(rayOrigin, rayDirection);
    vec3 color = res.col;



     color /= (color + vec3(1.0));


     // focus / blur

      //vec3 ufo = ufoPos();
       //focus = abs(length(res.firstJumpPos - ufo)) * 0.005 + 0.01;

      //float t4 = max(0, iTime - P_3);
      //focus = mix(focus, 1 - smoothstep(0, 1, t4), t4); // make clearer as ufo ascends


    fragColor = vec4(pow(color, vec3(0.5)), clamp(focus, 0.001, 2.0));
    fragColor.xyz *= smoothstep(0.0, 0.5, iTime);
}

)""

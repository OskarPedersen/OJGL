R""(
const float S_distanceEpsilon = 1e-2;
const float S_normalEpsilon = 1e-3;
const int S_maxSteps = 400;
const float S_maxDistance = 100.0;
const float S_distanceMultiplier = 0.5;
const float S_minVolumetricJumpDistance = 0.02;
const float S_volumetricDistanceMultiplier = 0.75;
const int S_reflectionJumps = 2;

#define S_VOLUMETRIC 0
#define S_REFLECTIONS 1

#include "common/primitives.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;
uniform float mBassdrum;
uniform float mBassdrumTot;
uniform float mHihat;
uniform float mHihatTot;
uniform float mSnare;
uniform float mSnareTot;
uniform float mStrings;
uniform float mStringsTot;

const int sphereType = 1;
const int roomType = 2;
const int armBodyType = 3;
const int armJointType = 4;
const int oskarType = 5;
const int screenType = 6;

vec3 gEye;
float gFresnel = 0.0;
float gFocusDistance = 0.0;
const vec3 S_focusTarget = vec3(0.0, 0.0, 0.0);
const float S_focusStrength = 0.03;
vec3 cRoomSize = vec3(20, 20, 20);
float floorPosition = 0;

float GridPattern(in vec2 uv)
{
  return 0.5*clamp(10.*sin(PI*uv.x) + 10.5, 0.0, 1.0)
       / 0.5*clamp(10.*sin(PI*uv.y) + 10.5, 0.0, 1.0);
}

float SquareHolePattern(in vec2 uv)
{
  float thickness = 4.0;
  float t = cos(uv.x*2.0) * cos(uv.y*2.0) / thickness;
  return smoothstep(0.1, 0.0, t*t);
}

DistanceInfo scene1(in vec3 p)
{
    p.y -= 5.5;
    p.x -= 5.0;
    // pMod1(p.x, 5.0);
    // pMod1(p.z, 15.0);
    float d1 = sdSphere(p, 1.0);
    p.x -= 0.5 * cos(3*iTime);
    p.z -= 0.5 * sin(3*iTime);
    float d2 = sdCutSphere(p, 1.0, 0.0);
    float d = smink(d1, d2, 0.8);
    return DistanceInfo(d, sphereType);
}

float wallPattern(in vec2 uv) {
    float thickness = 2.0;
    float t = cos(uv.x*2.0) * cos(uv.y*2.0) / thickness;
    return smoothstep(0.1, 0.0, t*t);
}

DistanceInfo room(in vec3 p)
{
    p.y += floorPosition -cRoomSize.y;
    p.y -= wallPattern(p.xz) * 0.005;
    p.x -= wallPattern(p.zy) * 0.005;
    p.z -= wallPattern(p.xy) * 0.005;
    return DistanceInfo(-sdBox(p, cRoomSize), roomType);
}

float func(float n) {
    return 0.8 * sin(0.3 * n*2);
}

DistanceInfo robotArm(in vec3 p)
{
    float aBase =     0.9 * sin(0.35 * iTime);
    float aShoulder = 0.35 + 0.3 * sin(0.5 * iTime + 1.0);
    float aElbow =    0.9 + 0.5 * sin(0.45 * iTime + 2.5);
    float aWrist =    0.8 * sin(0.7 * iTime);

    float x0 = func(mStringsTot - 1);
    float x1 = func(mStringsTot);
    float aFinger = mix(x0, x1, smoothstep(0.0, 0.15, mStrings));

    float dJoint = sdCappedCylinder(p - vec3(0.0, 0.13, 0.0), vec2(0.8, 0.1));

    // Axis 1
    vec3 q = p - vec3(0.0, 0.65, 0.0);
    q.xz *= rot(aBase);
    float dBody = sdRoundBox(q - vec3(0.0, 0.0, 0.0), vec3(0.15, 0.45, 0.23), 0.08);

    // Axis 2
    vec3 s = q - vec3(0.0, 0.55, 0.0);
    dJoint = min(dJoint, sdCappedCylinder(s.xzy, vec2(0.28, 0.32)));
    vec3 la = s;
    la.xy *= rot(aShoulder);
    float lowerLen = 1.5;
    dBody = smink(dBody, sdRoundBox(la - vec3(0.0, 0.5 * lowerLen, 0.0), vec3(0.12, 0.5 * lowerLen, 0.14), 0.07), 0.1);

    // Axis 3
    vec3 e = la - vec3(0.0, lowerLen, 0.0);
    dJoint = min(dJoint, sdCappedCylinder(e.xzy, vec2(0.22, 0.27)));
    vec3 ua = e;
    ua.xy *= rot(aElbow);
    float upperLen = 1.2;
    dBody = min(dBody, sdRoundCone(ua, 0.17, 0.11, upperLen));

    // Axis 5
    vec3 w = ua - vec3(0.0, upperLen, 0.0);
    dJoint = min(dJoint, sdCappedCylinder(w.xzy, vec2(0.13, 0.16)));
    vec3 tool = w;
    tool.xy *= rot(aWrist);
    pModPolar(tool.xz, 4);
    tool.x -= 0.1;
    tool.xy *= rot(-0.2);

    float fLength = 0.2;
    float fRadius = 0.03;
    dJoint = min(dJoint, sdCappedCylinder(tool - vec3(0.0, 0.12 + fLength*0.5, 0.0), vec2(fRadius, fLength)));
    
    // Axis 6
    vec3 qq = tool - vec3(0.0, 0.4, 0);
    dJoint = min(dJoint, sdCappedCylinder(qq.xzy, vec2(0.04, 0.04)));
    qq.xy *= rot(aFinger);

    dJoint = min(dJoint, sdRoundCone(qq - vec3(0.00, 0.01, 0.0), 0.025, 0.005, fLength*2));
    

    return un(DistanceInfo(dBody, armBodyType), DistanceInfo(dJoint, armJointType)); 
}

float udRoundBox( vec3 p, vec3 b, float r )
{
  return length(max(abs(p)-b,0.0))-r;
}

float llt(vec3 p) 
{
    p *= 0.3;

    pModPolar(p.xz, 6);
    p.x -= 3;
	
    // base ?
    float w = 0.2;
    if (p.y < 0.5) {
    	w += 0.4 * (0.5 - p.y);
    }
    if (p.y > 2.7) {
    	w -= (p.y - 2.7) * 0.5;
    }
    float d = sdHexPrism((p - vec3(0.0, 1.5, 0.0)).xzy, vec2(w, 1.5));
    float res = d;
    
    p.xz *= rot(iTime * 1.5);

    { // top ? 
        float w = 0.2;
        w -= abs(p.y - 3.3) * 0.1;
        float a = 0.2;
        if (p.x < 0.0) {
        	a -= abs(p.y - 3.3) * 0.1;
        }

    	float d = sdBox(p - vec3(0.0, 3.3, 0.0), vec3(a, 0.3, w));
    	res = min(d, res);
    }
    { // barrell ?
        float d = sdCappedCylinder((p - vec3(0.5, 3.3, 0.0)).yxz, vec2(0.05, 0.3));
        d = max(d, -sdCappedCylinder((p - vec3(0.5, 3.3, 0.0)).yxz, vec2(0.03, 0.8)));
        res = min(d, res);
    
    }
    return res.x;
}

float missile(vec3 p) {
    p *= 0.2;
    vec2 b = pMod2(p.xz, vec2(3));
    p.y -= mod((b.x + b.y)*6.0 + iTime * 10.0, 10.0);

    float w = 0.2 - 0.03 * smoothstep(1.4, 1.6, p.y);
    if (p.y > 1.7) {
     	w -= 0.2 * (p.y - 1.7);
    }
   
    float d = sdCappedCylinder(p, vec2(w, 2));
    float ds = sdSphere(p - vec3(0, 2.0, 0.0), 0.11);
    d = min(ds, d);
    float bw = 0.1;
    if (p.y < -2.05) {
    	bw += 0.6 * (-p.y - 2.05);
    }
    float bot = sdCappedCylinder(p - vec3(0,-2.05,0), vec2(bw, 0.1));
    d = min(bot, d);
    return d;
}

float opSubtraction( float d1, float d2 )
{
    return max(-d1,d2);
}


float opIntersection( float d1, float d2 )
{
    return max(d1,d2);
}

DistanceInfo oskar(in vec3 p) {
    float phase = mod(mBassdrumTot, 4);
    if (phase >= 3 ) { // waves w rocket
        float d1 = p.y - 3 + sin(p.x + iTime * 5) +  0.1 * sin(p.x * 3 + iTime * 3);
        float d2 = missile(p);
        return DistanceInfo(smink(d1, d2, 1.8), oskarType);

    } else if (phase >= 2 ) { // llt
        float d1 = llt(p);
        float d2 = sdSphere(p, 7.0);
        return DistanceInfo(min(d1, d2), oskarType);

    } else if (phase >= 1 ) { // tower w spheres
        vec3 q = p;
        float b = pMod1(q.y, 2);
        float a = pModPolar(q.xz, 12);
        q -= vec3(2 +  max(0, sin(iTime * 8 + b)), 0, 0);
        float d1 = sdSphere(q , 0.5);
        float d2 = sdCylinder(p.xzy, 1.5);
        return DistanceInfo(min(d1, d2), oskarType);

    } else { // screen
        vec3 o = p;
        vec2 a = pMod2(p.xz, vec2(3));
        p.y -= (4.0 - 0.5*(abs(a.x) + abs(a.y)))*mHihat + 0.1;
        float d1 = sdBox(p, vec3(1.2, 0.1, 1.2));

        float d2 = sdBox(o, vec3(16.5));
        float d = opIntersection(d1, d2);
        return DistanceInfo(d, screenType);
    }
}

DistanceInfo map(in vec3 p)
{
    return un(room(p), oskar(p));
    //return un(scene1(p), un(robotArm(p), room(p)));
}

const float roomEdgeBevel = 2.7;
float roomEdgeAmount(in vec3 p)
{
    p.y += floorPosition -cRoomSize.y;
    vec3 q = p;
    vec3 d = abs(q) - cRoomSize;
    vec3 w = smoothstep(-roomEdgeBevel, 0.0, d);
    return clamp(w.x + w.y + w.z - 1.0, 0.0, 1.0);
}

float getReflectiveIndex(int type)
{
    float pulse = exp(-mBassdrum * 6.0);
    if (type == sphereType){
        return mix(pulse, 0.9, gFresnel);
    }
    if (type == armBodyType) {
        return 0.1;
    }
    if (type == armJointType) {
        return 0.25;
    }
    if (type == oskarType) {
        return 0.5;
    }
    if (type == screenType) {
        return 0.5;
    }
    return 0.0;
}

vec3 getColor(in MarchResult result)
{
    if (result.jump == 0) {
        gFocusDistance = length(gEye - result.position);
    }

    if (result.type == invalidType) {
        return vec3(0.0);
    }

    float pulse = exp(-mBassdrum * 6.0);
    vec3 lightPosition = vec3(15 * (1 - 2*pulse), 4.0, 3.0);
    vec3 normal = normal(result.position);
    vec3 invLight = normalize(lightPosition - result.position);
    vec3 viewDir = normalize(gEye - result.position);
    float diffuse = max(0.0, dot(invLight, normal));
    vec3 halfDir = normalize(invLight + viewDir);
    float specular = pow(max(0.0, dot(normal, halfDir)), 32.0);

    if (result.type == sphereType || result.type == oskarType) {
        float pulse = exp(-mBassdrum * 6.0);
        vec3 metalColor = 0.5*vec3(0.2, 0.5, 0.9);
        gFresnel = pow(1.0 - max(0.0, dot(normal, viewDir)), 4.0);
        vec3 baseColor = metalColor * diffuse * (1.0 + 2.0 * pulse);
        vec3 tintedSpecular = specular * mix(vec3(1.0), metalColor, 0.6); 
        return baseColor + 2.0 * gFresnel * mix(vec3(0.6, 0.8, 1.0), metalColor, 0.4) + tintedSpecular;
    } else if (result.type == screenType) {
        float pulse = exp(-mBassdrum * 6.0);
        vec2 copy = result.position.xz;
        vec2 a = pMod2(copy, vec2(3));
        //vec3 color = vec3(a.x, a.y, 0.0);
        vec3 color = vec3(0.0);
        // -5 to 5
        float x = a.x + 5.0;
        float z = a.y + 5.0;
        vec3 c1 = vec3(0.3, 0.8, 0.8);
        vec3 c2 = vec3(0.2, 0.3, 0.6);
        if (mod(mHihatTot, 2.0) >= 1.0) {
            c2 = vec3(0.3, 0.8, 0.8);
            c1 = vec3(0.2, 0.3, 0.6);
        }
        if (x == 0 || x == 10 || z == 0 || z == 10) {
            color = c1;
        } else if (x == 1 || x == 9 || z == 1 || z == 9) {
             color = c2;
        } else if (x == 2 || x == 8 || z == 2 || z == 8) {
            color = c1;
        } else if (x == 3 || x == 7 || z == 3 || z == 7) {
            color = c2;
        } else if (x == 4 || x == 6 || z == 4 || z == 6) {
            color = c1;
        } else {
            color = c2;
        }
        gFresnel = pow(1.0 - max(0.0, dot(normal, viewDir)), 4.0);
        vec3 baseColor = color * diffuse * (1.0 + 2.0 * pulse);
        vec3 tintedSpecular = specular * mix(vec3(1.0), color, 0.6); 
        return baseColor + 2.0 * gFresnel * mix(vec3(0.6, 0.8, 1.0), color, 0.4) + tintedSpecular;
    } else if (result.type == armBodyType ) {
        vec3 bodyColor = 0.8*vec3(0.2, 0.5, 0.9);
        gFresnel = 0.3 * pow(1.0 - max(0.0, dot(normal, viewDir)), 4.0);
        return bodyColor * (0.06 + diffuse) + 0.8 * specular * mix(vec3(1.0), bodyColor, 0.3) + gFresnel * vec3(1.0, 0.55, 0.25);
    } else if (result.type == armJointType) {
        vec3 graphite = 0.5*vec3(0.08, 0.08, 0.09);
        gFresnel = 0.2 * pow(1.0 - max(0.0, dot(normal, viewDir)), 4.0) * step(0.1, result.position.y);
        return graphite * (0.3 + diffuse) + 0.6 * specular + gFresnel * vec3(0.4);
    } else {
        float edgeAmount = roomEdgeAmount(result.position);
        vec3 metalColor = 0.3*vec3(0.2, 0.3, 0.3);
        gFresnel = 0.1*pow(1.0 - max(0.0, dot(normal, viewDir)), 4.0);
        vec3 baseColor = metalColor * diffuse;
        vec3 tintedSpecular = specular * mix(vec3(1.0), metalColor, 0.6);
        vec3 col = baseColor + 2.0 * gFresnel * metalColor + tintedSpecular;
        float shadow = 0.3 + 0.7*shadowFunction(result.position, lightPosition, 30);
        return shadow * col * (1.0 - 0.4*edgeAmount);

    }
}

VolumetricResult evaluateLight(in vec3 p)
{
    return VolumetricResult(1e6, vec3(0.0));
}

float getFogAmount(in vec3 p)
{
    return 0.0;
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -1.0, 1.0)).xyz;
    gEye = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 rayDirection = normalize(rayOrigin - gEye);

    vec3 color = march(rayOrigin, rayDirection);

    float focalDistance = length(gEye - S_focusTarget);
    float focus = clamp( (abs(gFocusDistance - focalDistance)) * S_focusStrength, 0.0, 1.0);

    fragColor = vec4(pow(max(color, 0.0), vec3(0.4545)), focus);
}
)""

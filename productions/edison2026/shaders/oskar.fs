R""(
const float S_distanceEpsilon = 1e-2;
const float S_normalEpsilon = 1e-3;
const int S_maxSteps = 400;
const float S_maxDistance = 100.0;
const float S_distanceMultiplier = 0.9;
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

DistanceInfo map(in vec3 p)
{
    return un(scene1(p), un(robotArm(p), room(p)));
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

    if (result.type == sphereType) {
        float pulse = exp(-mBassdrum * 6.0);
        vec3 metalColor = 0.5*vec3(0.2, 0.5, 0.9);
        gFresnel = pow(1.0 - max(0.0, dot(normal, viewDir)), 4.0);
        vec3 baseColor = metalColor * diffuse * (1.0 + 2.0 * pulse);
        vec3 tintedSpecular = specular * mix(vec3(1.0), metalColor, 0.6); 
        return baseColor + 2.0 * gFresnel * mix(vec3(0.6, 0.8, 1.0), metalColor, 0.4) + tintedSpecular;
    } else if (result.type == armBodyType) {
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

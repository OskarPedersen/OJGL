R""(

float S_distanceEpsilon = 1e-3;
const float S_normalEpsilon = 5e-2;
const int S_maxSteps = 600;
const float S_maxDistance = 800.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.02;
const float S_volumetricDistanceMultiplier = 0.75;
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
uniform sampler2D borgilaTexture;
uniform sampler2D inTexture0;
uniform sampler2D inTexture1;
uniform sampler2D inTexture2;
uniform sampler2D inTexture3;
uniform sampler2D inTexture4;

const int boatType = 1;
const int mountainType = 2;
const int lissajousType = 3;
const int waterType = 4;
const int instrumentPanelType = 5;
const int hullType = 6;
const int screenType = 7;
const int ufoType = 8;

vec3 cameraPosition;
vec3 rayDirection;

bool willHitText = false;
float lissajousStrength = 0;

vec3 boatPosition;
float boatRotation;
float screenRotation = 0;

vec3 getAmbientColor(int type, vec3 pos, vec3 normal)
{
    switch (type) {
        case boatType:
            return  1.5*vec3(1, 1, 1);
        case mountainType: 
            return 0.0*vec3(0.2, 0.2, 0.8);
        case waterType:
            return vec3(0.1, 0.1, 0.7);
        case instrumentPanelType:
            return vec3(0.1);
        case screenType:
            return vec3(0.02);
        case lissajousType:
            return vec3(0.0, 0.0, 0.0);
        case hullType:
            return vec3(0.0);
        case ufoType:
            return vec3(5.0);
        default:
           return 7.0*vec3(0, 0.0, 1);
    }
}

float specularIndex(int type) {
    switch(type) {
        case screenType:
            return 0.0;
        default:
            return 1.0;
    }    
}

float getFogAmount(in vec3 p)
{
    return 0.001;
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
    float spec = specularIndex(result.type) * pow(k, 30.0);
    color += spec;

    float aof = result.type == screenType || result.type == instrumentPanelType ? 0.0 : 0.75;

    if (result.type == invalidType && result.jump == 0) {
        float pitch = asin(rayDirection.y);
        float yaw = atan(rayDirection.z, rayDirection.x);

        vec2 uv;
        uv.x = (yaw + PI) / (2.0 * PI);
        uv.y = (pitch + PI / 2.0) / PI;
        float h = texture(inTexture4, uv * 5).x + 0.05*hash11(fragCoord.x*fragCoord.y);
        
        color = mix(color, ao, aof);
        color *= 0.35*(1 - 0.6*smoothstep(18, 23, iTime));
        color = mix(color, color + 0.3*vec3(clamp(h, 0.0, 1.0)), h);
        return color;
    } else {
        return result.scatteredLight + result.transmittance *  mix(color, ao, aof);

    }

}

float getReflectiveIndex(int type) {
    switch (type) {
        case boatType:
            return 1.0;
        case mountainType:
            return 0.0;
        case waterType:
            return 1.0;
        case screenType:
            return 0.2;
        case instrumentPanelType:
            return 0.5;
        case hullType:
            return 0.1;
        case ufoType:
            return 0.1;
        default:
           return 0.0;
    }
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
    float t = 0;//mod(2*iTime, 40.0);
    float h = 4*texture(inTexture0, (p.xz)/90.0).x + 
              200*pow(texture(inTexture0, (p.xz+t )/1600.0).x, 4);

    float d = p.y - h + 10;

	return d;
}


float instrumentPanel(vec3 p)
{
    vec3 po = p;
    p.xz *= rot(boatRotation);
    p.y -= -0.05;
    p.z -= 2.0;

    float d = sdBox(p, vec3(6.0, 1.0, 4.0)); 
    p = po;
    p.y -= 1.5;
    p.zy *= rot(screenRotation);
    p.xz *= rot(boatRotation);

    
    p.x = abs(p.x);
    float d1 = sdBox(p - vec3(1, 0, 1), vec3(0.01, 1.0, 0.001));
    d = min(d, d1);
    
    p = po;

    p.y -= 1.0;
    p.zy *= rot(screenRotation);
    p.xz *= rot(boatRotation);
    float d3 = sdBox(p, vec3(7.0, 0.66, 1.0));
    d = min(d, d3);

    return d;
}

float screens(vec3 p) {
    vec3 po = p;
    p.y -= 1.5;
    p.zy *= rot(screenRotation);
    p.xz *= rot(boatRotation);

    float d2 = sdBox(p, vec3(3.0, 1.0, 1.0)); 
    p = po;
    return d2;

}

float hull(vec3 p) {
    vec3 po2 = p;
    p.x *= 1.5;
    vec3 po = p;
    p.y += 0.0;
    p.z -= 2.0;
    float bd = sdCappedCylinder(p, vec2(10,2));
    
    p = po.zxy;
    p.x += 8.43;
    float cd = sdCylinder(p, 9.0);
    p = po;
    p.y += 5.0;
    float ed = sdBox(p, vec3(5.0));

    p = po2;
    p.x = abs(p.x);
    p.y -= 2.3;
    p.x -= 4.0;
    p.z += 3.6;
    p.zy *= rot(3.1);
    float d5 = sdBox(p, vec3(5.0, 5.6, 2.2));
    bd = min(bd, d5);
    
    p = po2;
    p.y -= 6.85;
    p.z+=3;
    p.xz *= rot(boatRotation);
    float d4 = sdBox(p, vec3(18.0, 1.6, 3.0));
    ed = min(ed, d4);

    p = po2;
    p.y -= 7.5;
    p.z+=6;
    float d6 = sdBox(p, vec3(10.0, 15.6, 0.2));
    ed = min(ed, d6);

    p = po2;
    p = p.xzy;
    p.y-=5;
    float d7 = sdCylinder(p, 0.3);
    ed = min(ed, d7);

    return min(ed, max(bd, -cd));
}


float uvBox(vec3 p, vec3 b, inout vec2 uv)
{
    vec3 d = abs(p) - b;
    float dis = length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);

    uv = p.xy / (b.xy * 2) - 0.5;
    uv.x*=-1;
    return dis;
}

float lissajous(vec3 p)
{
    p.y -= 1.5;
    p.zy *= rot(screenRotation);
    p.xz *= rot(boatRotation);

    p.z -= 1;
    vec2 uv;
    float d =  uvBox(p, vec3(0.8, 0.8, 0.0), uv);
    uv.x *=-1;
    if ( d < 0.01) {
        float s = texture(inTexture1, uv).x;
        if (s < 0.001) { // If not on text
            d = 0.1;
        }

        if (d < 0.01)
            lissajousStrength = s;

	}
    return d;
}

float radar(vec3 p)
{
    p.y -= 1.5;
    p.zy *= rot(screenRotation);
    p.xz *= rot(boatRotation);
    p.z -= 1;
    p.x -= 2.0;
    vec2 uv;
    float d =  uvBox(p, vec3(0.8, 0.8, 0.0), uv);
    uv.x *=-1;
    if ( d < 0.01) {
        float s = texture(inTexture2, uv).x;
        if (s < 0.001) { // If not on text
            d = 0.1;
        }
        if (d < 0.01)
            lissajousStrength = s;

	}
    return d;
}

float ojText(vec3 p)
{
    p.y -= 1.5;
    p.zy *= rot(screenRotation);
    p.xz *= rot(boatRotation);
    p.z -= 1;
    p.x -= -2.0;
    vec2 uv;
    float d =  uvBox(p, vec3(0.98, 0.98, 0.0), uv);
    uv.x *=-1;
    if ( d < 0.01) {
        float s = texture(inTexture3, uv).x;
        if (s < 0.001) { // If not on text
            d = 0.1;
        }
        if (d < 0.01)
            lissajousStrength = s;

	}
    return d;
}

float ufo(vec3 p) {
    if (iTime > 15.0) {
        float heading = -3.1415;
        float l = 0;
        p -= vec3(-46.524 -l, 50.38, 121.575 + l);
        float s = 0.8*smoothstep(19.0, 28.0, iTime);
        //float d2 = sdTorus(p - vec3(0, -3*s, 0), vec2(s*8.5, 0.5));
        //float d1 = length(p) - 2.0 * s;
        //return min(d1, d2);


        

        float d2 = sdTorus(p - vec3(0, -3*s, 0), vec2(8.5*s, 0.5));

        float d1 = length(p) - 2.0*s;

        float d3 = sdTorus(p - vec3(0, -0.5, 0), vec2(1.5*s, 0.5*s));

        //p.xz *= rot(iTime * 2.0);
        pModPolar(p.xz, 16);
        float d4 = length(p - vec3(8.9*s, -3*s, 0)) - 0.3*s;
        return min(smink(d2, d4, 0.7), smink(d1, d3, 1.5));
    } else {
        return 100.0;
    }

}

DistanceInfo map(vec3 p)
{
   S_distanceEpsilon = 1e-3 + (1e-1)*(smoothstep(100, 400, length(p)));
   DistanceInfo box = {mountain(p), mountainType};
   DistanceInfo waterInfo = {water(p), waterType};
   DistanceInfo ufoInfo = {ufo(p), ufoType};

   p -= boatPosition;
   DistanceInfo sphereInfo = {instrumentPanel(p), instrumentPanelType};
   DistanceInfo screenInfo = {screens(p), screenType};
   DistanceInfo hullInfo = {hull(p), hullType};
   return un(ufoInfo, un(screenInfo, un(hullInfo, un(waterInfo, un(box, sphereInfo)))));
}

VolumetricResult evaluateLight(in vec3 p)
{
    p -= boatPosition;
    vec3 color = vec3(0.1, 0.9, 0.1);
    float d = lissajous(p);
    d = min(d, radar(p));
    d = min(d, ojText(p));
    float str = lissajousStrength;
    vec3 res = color * str / 0.001;
    return VolumetricResult(d, res); 
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -0.52, 1.0)).xyz;
    cameraPosition = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1)).xyz;
    rayDirection = normalize(rayOrigin - cameraPosition);

    boatRotation = -3.1415;

    boatPosition = vec3(21.9963, 0.0, -72.8188 + 5.5);
    float speed = 1.0f;
    boatPosition.x += speed * sin(boatRotation) * iTime;
    boatPosition.z += -speed * cos(boatRotation) * iTime;
    // boatPosition = vec3(iCameraMatrix[3][0], 0.0, iCameraMatrix[3][2] + 5.5);
    boatPosition += vec3(0.05 * sin(iTime), 0.05 * sin(iTime + 3), 0.05 * sin(iTime + 5));

    screenRotation = -0.4;
    vec3 color = march(rayOrigin, rayDirection);
    // color /= (color + vec3(1.0));

    color *= smoothstep(0, 1, iTime);
    fragColor = vec4(pow(color, vec3(0.5)), 1.0);
}

)""

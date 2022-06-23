R""(

const float S_distanceEpsilon = 1e-2;
const float S_normalEpsilon = 1e-2;
const int S_maxSteps = 150;
const float S_maxDistance = 220.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
const float S_volumetricDistanceMultiplier = 0.75;
const int S_reflectionJumps = 3;

#define S_VOLUMETRIC 1
#define S_REFLECTIONS 1

#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;

const int shipType = 1;
const int waterType = 2;
const int skyType = 3;

vec3 shipPos = vec3(0, 0, 3);

bool isGhost() {
    return mod(iTime, 2) > 1;
}

float shipDistance(in vec3 p) {
    vec3 rp = p - shipPos;
    float len = 0.7;
    float height = 0.1;
    float width = 0.2 + rp.y * 0.5;
    if (rp.x < -0.1) {
        width += rp.x + 0.1;
    }
    float d = sdRoundBox(rp , vec3(len, height, width), 0.0);
    d = max(-sdRoundBox(rp - vec3(0.55, 0.1, 0) , vec3(0.1, 0.1, 0.15), 0.0), d);

    // top
    if (rp.y > 0.2) {
        width -= rp.y - 0.2;
    }

    float d2 = sdRoundBox(rp - vec3(0.1, height, 0), vec3(0.3, 0.2, width), 0.0);

    d = min(d, d2);

    // antenna
    float w = 0.02 + 0.5 - p.y * 0.2 - 0.4;
    float d3 = sdRoundBox(rp - vec3(0, 0.22, 0), vec3(w, 0.3, w), 0.0);

    d = smink(d, d3, 0.05);

    return d;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 rp = p - shipPos;
    vec3 orp = rp;

    rp.xz *= rot(iTime);

    float d = sdCappedCylinder(rp.xzy - vec3(0, 0, 0.55), vec2(0.01, 0.2));

    if (isGhost()) {
        d = shipDistance(p);
    }



    float ds = length(rp - vec3(0, 0.5, 0));
    float strength = 500;
	vec3 col = vec3(0.05, 1.0, 0.05);
	vec3 res = col * strength / (d * d);

	return VolumetricResult(d, res);
}

float getFogAmount(in vec3 p)
{
    return 0.00005;
}

DistanceInfo map(in vec3 p, bool isMarch)
{
    DistanceInfo shipDI = DistanceInfo(shipDistance(p), shipType, vec3(1, 2, 3));



    // water
    float wn = exp2(noise_2(p.xz * 10 - vec2(iTime, iTime*0.2)) + noise_2(p.xz * 5 - vec2(iTime * 0.2, iTime)));
    float wd = p.y + 0.05 + wn * 0.05;
    DistanceInfo waterDI = {wd, waterType, vec3(1, 2, 3)};

    DistanceInfo res = waterDI; //
    if (!isGhost()) {
        res = un(shipDI, waterDI);
    }

    // sky
    float sd = abs(p.y - 7.0);
    res = un(DistanceInfo(sd, skyType, vec3(1, 2, 3)), res);


    return res;
}

float getReflectiveIndex(int type)
{
    if(type == shipType)
        return 0.5;
    if(type == waterType)
        return 0.9;
    return 0.0;
}

vec3 getColor(in MarchResult result)
{
    vec3 col = vec3(1, 1, 1);
    if (result.type == waterType) {
        col = vec3(0, 0, 1);
    }
    else if (result.type == skyType) {
        col = vec3(0, 0, 1);
    }

    vec3 invLight = -normalize(vec3(0.7, -0.2, 0.5));
    vec3 normal = normal(result.position);
    float diffuse = max(0., dot(invLight, normal));
    return result.scatteredLight +  result.transmittance * col * diffuse * 1.0;
}


void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;


    vec3 eye = vec3(sin(iTime * 0.3) * 2.0, 2, 0);
    vec3 tar =  vec3(0, 0, 3);
    vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	vec3 up = cross(dir, right);

    vec3 rd = normalize(dir + right*u + up*v);


    vec3 color = march(eye, rd);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.5)), 1.0);
}

)""

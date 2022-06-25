R""(

const float S_distanceEpsilon = 1e-2;
const float S_normalEpsilon = 1e-2;
const int S_maxSteps = 150;
const float S_maxDistance = 200.0;
const float S_distanceMultiplier = 0.7;
const float S_minVolumetricJumpDistance = 0.005;
const float S_volumetricDistanceMultiplier = 0.1;//0.75;
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

const int shipTypeBottom = 1;
const int shipTypeTop = 2;
const int waterType = 3;
const int skyType = 4;

bool isGhost() {
    return false; //mod(iTime, 2) > 1;
}

bool isSinking() {
    return false;
}

vec3 shipPos(in vec3 p) {
    return p - vec3(0, 0, 3) + (isSinking() ? vec3(0, mod(iTime * 0.5, 1), 0) : vec3(0));
}

float shipDistance(in vec3 p) {
    vec3 rp = shipPos(p);
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
    float w = 0.02 + 0.5 - rp.y * 0.2 - 0.4;
    float d3 = sdRoundBox(rp - vec3(0, 0.22, 0), vec3(w, 0.3, w), 0.0);

    d = smink(d, d3, 0.05);

    return d;
}

VolumetricResult evaluateLight(in vec3 p)
{
    vec3 rp = shipPos(p);
    vec3 orp = rp;

   // rp.xz *= rot(iTime * (isGhost() ? 30.0 : 1.0));

    //float d = sdCappedCylinder(rp.xzy - vec3(0, 0, 0.55), vec2(0.01, 0.2));

    float d = length(rp - vec3(0, 0.55, 0)) - 0.05;

    if (isGhost()) {
        d = min(d, shipDistance(p));
    }


    //d = max(0.07, d);

    float ds = length(rp - vec3(0, 0.5, 0));
    float strength = 1500;// - 1500*sin(5*d + 3*sin(d) - 5*iTime);

    vec3 lightDir = normalize(vec3(sin(iTime), 0, cos(iTime)));
    vec3 posDir = normalize(p - vec3(0, 0.55, 3));
	float f = clamp(dot(lightDir, posDir), 0, 1);
    f = pow(f, 500);
    vec3 col = vec3(0.05, 1.0, 0.05);
	vec3 res = col * f * strength / (d * d);


    // ship lamps
    {
        float d2 = length(p - vec3(0, 0.55, 3)) - 0.03;
        float strength = 20;
        vec3 col = vec3(0.05, 1.0, 0.05);
        vec3 res2 = col * strength / (d2 * d2);

        d = min(d, d2);
        res += res2;
    }

	return VolumetricResult(d, res);
}

float getFogAmount(in vec3 p)
{
    return 0.0004;
}

DistanceInfo map(in vec3 p, bool isMarch)
{
    DistanceInfo shipDI = DistanceInfo(shipDistance(p), p.y < 0.11 ? shipTypeBottom : shipTypeTop, vec3(1, 2, 3));



    // water
    float wn = 0.05*sin(5*p.x + 3*p.z) + 0.06*(noise_2(p.xz * 10 - vec2(iTime, iTime*0.2)) + noise_2(p.xz * 5 - vec2(iTime * 0.2, iTime)));
    float wd = p.y + 0.05 + wn * 0.05;
    DistanceInfo waterDI = {wd, waterType, vec3(1, 2, 3)};

    DistanceInfo res = waterDI;
    if (!isGhost()) {
        if (isSinking()) {
            res = sunk(shipDI, waterDI, 0.1);
        } else {
            res = un(shipDI, waterDI);
        }
    }

    // sky
    float sd = abs(p.y - 7.0);
    res = un(DistanceInfo(sd, skyType, vec3(1, 2, 3)), res);


    return res;
}

float getReflectiveIndex(int type)
{
    if(type == shipTypeBottom) {
        return 0.03;
    } else if (type == shipTypeTop) {
        return 0.03;
    }
    if(type == waterType)
        return 0.5;
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
    else if(result.type == shipTypeBottom) {
        col = vec3(1);
    } else if (result.type == shipTypeTop) {
       col = vec3(1);
    }

    vec3 invLight = -normalize(vec3(0.7, -0.2, 0.5));
    vec3 normal = normal(result.position);
    float diffuse = max(0., dot(invLight, normal));
    return result.scatteredLight +  result.transmittance * col * diffuse * 0.0;
}


void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;


    vec3 eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
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

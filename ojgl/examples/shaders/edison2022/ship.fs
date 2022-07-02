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

#define PART_1_INTRO 10
#define PART_2_CHANNEL (PART_1_INTRO + 20)
#define PART_3_MISSILE (PART_2_CHANNEL + 20)

bool isGhost() {
    return true; //mod(iTime, 2) > 1;
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

VolumetricResult volumetricUn(in VolumetricResult vr1, in VolumetricResult vr2) {
    return VolumetricResult(min(vr1.distance, vr2.distance), vr1.color + vr2.color);
}

vec3 lightHouseColor = vec3(0.1, 0.2, 1.0);

VolumetricResult waterLights(in vec3 p) {
    p.x -= iTime * 0.3;
    float index = pMod1(p.x, 1.0);
    p.z -= 3;
    p.z = abs(p.z) - 1;
    float d = length(p);
    float strength = 1.5;
    vec3 res = lightHouseColor * strength / (d * d);

    return VolumetricResult(d, res);
}

vec3 missilePos() {
    const float time = iTime - PART_2_CHANNEL;

    vec3 pos = vec3(0, 0.6, -10);
    const float speed = 3.0;
    const float travel = time * speed;
    const float travelEndPoint = 13.5;
    pos.z += min(travelEndPoint, travel);

    if (travel > travelEndPoint) {
        float orbitTime = time - travelEndPoint / speed;
        pos.x += sin(orbitTime);
        pos.z += cos(orbitTime);
    }

    return pos;

    //return vec3(0, 0.6, 0) + vec3(0.1, 0, mod(iTime, 5) - 1);
}

VolumetricResult missile(in vec3 p) {
    //vec3 rp = shipPos(p);
    float d = length(p - missilePos());
    float strength = 20;
    vec3 res = vec3(0.3, 1.0, 0.3) * strength / (d * d);

    return VolumetricResult(d, res);
}

VolumetricResult lightHouseLight(in vec3 p) {
    vec3 rp = shipPos(p);

    float d = length(rp - vec3(0, 0.55, 0)) - 0.05;
    float ds = length(rp - vec3(0, 0.5, 0));
    float strength = 1500;// - 1500*sin(5*d + 3*sin(d) - 5*iTime);

    vec3 lightDir = normalize(vec3(sin(iTime), 0, cos(iTime)));
    vec3 posDir = normalize(p - vec3(0, 0.55, 3));
	float f = clamp(dot(lightDir, posDir), 0, 1);
    f = pow(f, 500);
    //vec3 col = vec3(0.05, 1.0, 0.05);
	vec3 res = lightHouseColor * f * strength / (d * d);

    return VolumetricResult(d, res);
}

VolumetricResult topLight(in vec3 p) {
    float d = length(p - vec3(0, 0.55, 3)) - 0.03;
    float strength = 20;
    //vec3 col = vec3(0.05, 1.0, 0.05);
    vec3 res = lightHouseColor * strength / (d * d);

    return VolumetricResult(d, res);
}

VolumetricResult evaluateLight(in vec3 p)
{

   // rp.xz *= rot(iTime * (isGhost() ? 30.0 : 1.0));

    //float d = sdCappedCylinder(rp.xzy - vec3(0, 0, 0.55), vec2(0.01, 0.2));

    //d = max(0.07, d);



    VolumetricResult res = lightHouseLight(p);

    res = volumetricUn(res, topLight(p));

    if (isGhost()) {
        float d = shipDistance(p);
        d = max(d, 0.02);
        float strength = 20;
        vec3 col = lightHouseColor * strength / (d * d);
        res = volumetricUn(res, VolumetricResult(d, col));
    }

    res = volumetricUn(res, waterLights(p));

    //VolumetricResult mis = missile(p);
    //res.distance = smink(res.distance, mis.distance, 0.1);
    //res.color = res.color + mis.color;

    if (iTime > PART_2_CHANNEL && iTime < PART_3_MISSILE) {
        res = volumetricUn(res, missile(p));
    }

	return res;
}

float getFogAmount(in vec3 p)
{
    return 0.0004;
}

DistanceInfo map(in vec3 p, bool isMarch)
{
    DistanceInfo shipDI = DistanceInfo(shipDistance(p), p.y < 0.11 ? shipTypeBottom : shipTypeTop, vec3(1, 2, 3));



    // water
    float wn = 0.03*(noise_2(p.xz * 10 - vec2(3*iTime, 0.3*iTime)) + noise_2(p.xz * 5 - vec2(3*iTime,  -0.3*iTime)));
    float wd = p.y + 0.05 + wn * 0.05;
    DistanceInfo waterDI = {wd, waterType, vec3(1, 2, 3)};

    DistanceInfo res = waterDI;
    //if (!isGhost()) {
        if (isSinking()) {
            res = sunk(shipDI, waterDI, 0.1);
        } else {
            res = un(shipDI, waterDI);
        }
    //}

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


vec3 march2(in vec3 rayOrigin, in vec3 rayDirection)
{
    float t = 0.0;
    vec3 scatteredLight = vec3(0.0);
    float transmittance = 1.0;
    float reflectionModifier = 1.0;
    vec3 resultColor = vec3(0.0);
    int jump = 0;
#if S_REFLECTIONS
    for (; jump < S_reflectionJumps; jump++) {
#endif
        for (int steps = 0; steps < S_maxSteps; ++steps) {
            vec3 p = rayOrigin + t * rayDirection;

            if (iTime > PART_2_CHANNEL && iTime < PART_3_MISSILE) {
                float d = length(p - missilePos());
                vec3 toMissile = normalize(p - missilePos());
                rayDirection += toMissile * 0.1 / (500.1 +  100*d * d * d);
                rayDirection = normalize(rayDirection);
            }

            DistanceInfo info = map(p, true);
            float jumpDistance = info.distance * S_distanceMultiplier;

#if S_VOLUMETRIC
            float fogAmount = getFogAmount(p);
            VolumetricResult vr = evaluateLight(p);

            float volumetricJumpDistance = max(S_minVolumetricJumpDistance, vr.distance * S_volumetricDistanceMultiplier);
            jumpDistance = min(jumpDistance, volumetricJumpDistance);
            vec3 lightIntegrated = vr.color - vr.color * exp(-fogAmount * jumpDistance);
            scatteredLight += transmittance * lightIntegrated;
            transmittance *= exp(-fogAmount * jumpDistance);
#endif

            t += jumpDistance;
            if (info.distance < S_distanceEpsilon) {
                vec3 color = getColor(MarchResult(info.type, p, steps, transmittance, scatteredLight, jump, rayDirection, info.color));
#if !S_REFLECTIONS
                return color;
#else
                t = 0.0;
                rayDirection = reflect(rayDirection, normal(p));
                rayOrigin = p + 0.1 * rayDirection;

                resultColor = mix(resultColor, color, reflectionModifier);
                reflectionModifier *= getReflectiveIndex(info.type);
                break;
 #endif
            }

            if (t > S_maxDistance || steps == S_maxDistance - 1) {
                vec3 color = getColor(MarchResult(invalidType, p, steps, transmittance, scatteredLight, jump, rayDirection, info.color));
                resultColor = mix(resultColor, color, reflectionModifier);
                return resultColor;
            }
        }
#if S_REFLECTIONS
    }
#endif

    return resultColor;
}


void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution. x;

    vec3 color = vec3(-1);
    vec3 eye = vec3(-1);
    vec3 tar = vec3(-1);

    if (iTime < PART_1_INTRO) {
        eye = vec3(-3, 2 + iTime * 0.5, 2.999);
        tar =  vec3(-3, 0, 3);
    } else if (iTime < PART_2_CHANNEL) {
        eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
        tar =  vec3(0, 0, 3);
    } else if (iTime < PART_3_MISSILE) {
        eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
        tar =  vec3(0, 0, 3);
    } else {
        //eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
        //tar =  vec3(0, 0, 3);
    }

    vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	vec3 up = cross(dir, right);

    vec3 rd = normalize(dir + right*u + up*v);

    color = march2(eye, rd);

    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.5)), 1.0);
}

)""

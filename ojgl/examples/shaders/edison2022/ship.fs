R""(

const float S_distanceEpsilon = 1e-2;
const float S_normalEpsilon = 1e-2;
const int S_maxSteps = 150;
const float S_maxDistance = 200.0;
const float S_distanceMultiplier = 1.0;
const float S_minVolumetricJumpDistance = 0.005;
const float S_volumetricDistanceMultiplier = 0.5;//0.75;
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

uniform float CHANNEL_2_SINCE; // 63 drum beat hi hat
uniform float CHANNEL_3_SINCE; // ambient synth
uniform float CHANNEL_4_SINCE; // kick drum
uniform float CHANNEL_8_SINCE; // drum beat snare
uniform float CHANNEL_9_SINCE; // drum beat crash
uniform float CHANNEL_6_SINCE; // 6 melody synth
uniform float CHANNEL_6_TOTAL;


const int shipTypeBottom = 1;
const int shipTypeTop = 2;
const int waterType = 3;
const int skyType = 4;

#define PART_1_INTRO 12
#define PART_2_CHANNEL (PART_1_INTRO + 10)
#define PART_3_MISSILE (PART_2_CHANNEL + 10)
#define PART_4_SINKING (PART_3_MISSILE + 17)

const float PART_3_speed = 3.0;
const float PART_3_travelEndPoint = 13.5;
const float PART_3_SPIN_START = PART_3_travelEndPoint / PART_3_speed;

const float PART_4_SINK_START = PART_3_MISSILE + 5.0;

bool isGhost() {
    if (iTime < PART_2_CHANNEL) {
        return true;
    } else if (iTime < PART_4_SINKING) {
        const float time = iTime - PART_2_CHANNEL;

        if (time > PART_3_SPIN_START) {
            return hash11(floor(time * 8.0)) > 0.5;
        } else {
            return true;
        }
    }
    return false; //mod(iTime, 2) > 1;
}

float shipLightStrengthModifier() {
        const float time = iTime - PART_2_CHANNEL;
        if (time > PART_3_SPIN_START) {
            if (!isGhost()) {
                return 0.0;
            } else {
                return 1.0 - smoothstep(PART_3_SPIN_START, PART_3_SPIN_START + 5.0, time);
            }
        } else {
            return 1.0;
        }
}

bool isSinking() {
    return iTime > PART_4_SINK_START;
}

vec3 shipPos(in vec3 p) {
    return p - vec3(0, 0, 3) + (isSinking() ? vec3(0, 0.1 * (iTime - PART_4_SINK_START), 0) : vec3(0));
}

float shipDistance(in vec3 p) {
    vec3 rp = shipPos(p);
    const float len = 0.7;
    const float height = 0.1;
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

    const float d2 = sdRoundBox(rp - vec3(0.1, height, 0), vec3(0.3, 0.2, width), 0.0);

    d = min(d, d2);

    // antenna
    const float w = 0.02 + 0.5 - rp.y * 0.2 - 0.4;
    const float d3 = sdRoundBox(rp - vec3(0, 0.22, 0), vec3(w, 0.3, w), 0.0);

    d = smink(d, d3, 0.05);

    return d;
}

VolumetricResult volumetricUn(in VolumetricResult vr1, in VolumetricResult vr2) {
    return VolumetricResult(min(vr1.distance, vr2.distance), vr1.color + vr2.color);
}

#define lightHouseColor vec3(0.1, 0.2, 1.0)

VolumetricResult waterLights(in vec3 p) {
    const float speed = 0.3;
    const float fullspeedDis = speed * PART_3_MISSILE;
    const float slowDis = 1.0;

    p.x -= min(fullspeedDis, iTime * speed) + max(0.0, 1.0 - pow(1.4, -(iTime - PART_3_MISSILE)));
    float index = pMod1(p.x, 1.0);
    p.z -= 3;
    p.z = abs(p.z) - 1;
    //float d = length(p);
    const float t = iTime - PART_4_SINK_START - 7.0;
    const float height = 0.075 - min(0.05, CHANNEL_2_SINCE * 0.5);
    float d = sdCappedCylinder(p + vec3(0, max(0, t * 0.1) + 0.05, 0), vec2(0.001, height));
    float strength = 20.5;
    vec3 res = lightHouseColor * strength / (d * d);

    return VolumetricResult(d, res);
}

const float PART_3_flyAwayStartTime = (PART_3_travelEndPoint / PART_3_speed) + 3 * (2 * PI) / PART_3_speed;

//#define MISSILE_TIME (iTime - PART_2_CHANNEL) //
#define MISSILE_TIME (0.75 * (CHANNEL_6_TOTAL * 0.5  + 0.5 * pow(CHANNEL_6_SINCE, 0.3)))
vec3 missilePos() {
    const float time = MISSILE_TIME;//iTime - PART_2_CHANNEL;

    vec3 pos = vec3(0.5, 0.6, -10);
    const float travel = PART_3_speed * time;
    pos.z += min(PART_3_travelEndPoint, travel);

    if (time > PART_3_flyAwayStartTime) {
        pos.z += (time - PART_3_flyAwayStartTime) * PART_3_speed;

    } else if (travel > PART_3_travelEndPoint) {
        float orbitTime = time - PART_3_travelEndPoint / PART_3_speed;
        pos.x += sin(-orbitTime * PART_3_speed + radians(90)) - 1;
        pos.z += cos(-orbitTime * PART_3_speed + radians(90));
    }

    return pos;

    //return vec3(0, 0.6, 0) + vec3(0.1, 0, mod(iTime, 5) - 1);
}

VolumetricResult missile(in vec3 p) {
    //vec3 rp = shipPos(p);
    float d = length(p - missilePos()) - 0.5;
    float strength = 20;
    vec3 res = vec3(0.4, 1.0, 0.4) * strength / (d * d);



    // pipe
    //float pd = sdCylinder(p - vec3(0.5, 0.6, 0), .01);

    //float pd = sdCappedCylinder((p - vec3(0.5, 0.6, 0)).xzy, vec2(0.01, 3.5));
    //float pd = sdCappedCylinder((p - vec3(0.5, 0.6, 10 + 3.5)).xzy, vec2(0.01, 10));

    const float r = 0.01;

    float pd = 9999999.0;
    float td = sdTorus(p - vec3(0.5 - 1, 0.6, -10 + PART_3_travelEndPoint), vec2(1.0 + sin(atan(p.x + 0.5, p.z - 3.5) * 20.0) * 0.05, r));

    //td = max(p.z - 3.5, td);
    //td = max(-(p.z - 3.5), td);

    //d = max(d, min(pd, td));

    const float time = MISSILE_TIME;//iTime - PART_2_CHANNEL;
    if (time < PART_3_SPIN_START + 1.0) {
        // first cylinder + half torus
        pd = sdCappedCylinder((p - vec3(0.5 + sin(p.z * 10.0) * 0.1, 0.6, 0)).xzy, vec2(r, 3.5));
        td = max(-(p.z - 3.5), td);
    } else if (time > PART_3_flyAwayStartTime) {
        // second cylinder + other half of the torus
        pd = sdCappedCylinder((p - vec3(0.5 + sin(p.z * 10.0) * 0.1, 0.6, 10 + 3.5)).xzy, vec2(r, 10));
        td = max(p.z - 3.5, td);
    }

    d = max(d, min(td, pd));
    strength = 0 + 30.0 - pow(CHANNEL_6_SINCE, 0.8) * 30.0;
    strength = max(0.0, strength);
    res = vec3(0.4, 1.0, 0.4) * strength / (d * d);

    //vr = volumetricUn(vr, VolumetricResult(d, res));

    VolumetricResult vr = { d, res };

    return vr;
}

VolumetricResult lightHouseLight(in vec3 p) {
    vec3 rp = shipPos(p);

    float d = length(rp - vec3(0, 0.55, 0)) - 0.05;
    vec3 boundCylPos = (rp - vec3(0, 0.55, 0));
    boundCylPos.xz *= rot(-iTime);
    float boundD = sdCappedCylinder(boundCylPos.xzy - vec3(0, 5.0, 0), vec2(0.0, 5.0));
    float ds = length(rp - vec3(0, 0.5, 0));
    float strength = 25000 * shipLightStrengthModifier();// - 1500*sin(5*d + 3*sin(d) - 5*iTime);

    vec3 lightDir = normalize(vec3(sin(iTime), 0, cos(iTime)));
    vec3 posDir = normalize(p - vec3(0, 0.55, 3));
	float f = clamp(dot(lightDir, posDir), 0, 1);
    f = pow(f, 1000);
    //d = boundD; f = 1.0;
    //vec3 col = vec3(0.05, 1.0, 0.05);
	vec3 res = lightHouseColor * f * strength / (d * d);


    return VolumetricResult(boundD, res);
}

VolumetricResult topLight(in vec3 p) {
    float d = length(p - vec3(0, 0.55, 3)) - 0.03;
    float strength = 50 * shipLightStrengthModifier();
    //vec3 col = vec3(0.05, 1.0, 0.05);
    vec3 res = lightHouseColor * strength / (d * d);

    return VolumetricResult(d, res);
}

VolumetricResult evaluateLight(in vec3 p)
{

   // rp.xz *= rot(iTime * (isGhost() ? 30.0 : 1.0));

    //float d = sdCappedCylinder(rp.xzy - vec3(0, 0, 0.55), vec2(0.01, 0.2));

    //d = max(0.07, d);



    VolumetricResult res = waterLights(p);



    //if (isGhost()) {
        float d = shipDistance(p);
        d = max(d, 0.02);
        float strength = 20 * shipLightStrengthModifier();

        vec3 col = lightHouseColor * strength / (d * d);
        res = volumetricUn(res, VolumetricResult(d, col));

        res = volumetricUn(res, lightHouseLight(p));
        res = volumetricUn(res, topLight(p));
    //}



    //VolumetricResult mis = missile(p);
    //res.distance = smink(res.distance, mis.distance, 0.1);
    //res.color = res.color + mis.color;

    if (iTime > PART_2_CHANNEL) {
        res = volumetricUn(res, missile(p));
    }

	return res;
}

float getFogAmount(in vec3 p)
{
    return 0.0001;
}

DistanceInfo map(in vec3 p, bool isMarch)
{
    DistanceInfo shipDI = DistanceInfo(shipDistance(p), p.y < 0.11 ? shipTypeBottom : shipTypeTop, vec3(1, 2, 3));



    // water
    float wn = 0.03*(noise_2(p.xz * 20 - vec2(5.0*iTime, 2.1*iTime)) + noise_2(p.xz * 15 - vec2(4.0*iTime,  -2.1*iTime)));
    float wd = p.y + 0.0 + pow(wn, 0.1) * 0.1;
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
    //float sd = abs(p.y - 7.0);
    //res = un(DistanceInfo(sd, skyType, vec3(1, 2, 3)), res);


    return res;
}

//float getReflectiveIndex(int type)
//{
//    if(type == shipTypeBottom || type == shipTypeTop) {
//        return isGhost() ? 0.5 : 0.5;
//    }
//    if(type == waterType)
//        return 0.5;
//    return 0.5;
//}

float specular(vec3 normal, vec3 light, vec3 viewdir, float s)
{
	//float nrm = (s + 8.0) / (3.1415 * 8.0);
	float k = max(0.0, dot(viewdir, reflect(light, normal)));
    return  pow(k, s);
}

vec3 getColor(in MarchResult result, vec3 eye)
{
    //vec3 col = vec3(1, 1, 1);
    //if (result.type == waterType) {
    //    col = vec3(0, 0, 1);
    //}
    //else if (result.type == skyType) {
    //    col = vec3(0, 0, 1);
    //}
    //else if(result.type == shipTypeBottom) {
    //    col = vec3(1);
    //} else if (result.type == shipTypeTop) {
    //   col = vec3(1);
    //}


    vec3 normal = normal(result.position);
    //float diffuse = max(0., dot(invLight, normal));

    float specTotal = 0.0;
    if (isGhost()) {
        vec3 lightPos = vec3(0, 2, 3);
        vec3 invLight = normalize(lightPos - result.position);

        float spec = specular(normal, -invLight, normalize(eye - result.position), 2000.0);
        const float dis = length(lightPos - result.position);
        const float specStr = 30.0 / (dis * dis);
        specTotal += spec * specStr * shipLightStrengthModifier();
    }


    // missile spec
    vec3 lightPos = missilePos();
    vec3 invLight = normalize(lightPos - result.position);

    float spec = specular(normal, -invLight, normalize(eye - result.position), 2000.0);
    const float dis = length(lightPos - result.position);
    const float specStr = 3.0 / (dis * dis);
    specTotal += spec * specStr;

    return result.scatteredLight +  result.transmittance * vec3(specTotal);
    //return result.scatteredLight +  result.transmittance * col * diffuse * 0.0 + vec3(spec);
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
    int jump = 0;
#if S_REFLECTIONS
    for (; jump < S_reflectionJumps; jump++) {
#endif
        for (int steps = 0; steps < S_maxSteps * (jump == 0 ? 1.0 : 0.5); ++steps) {
            vec3 p = rayOrigin + t * rayDirection;

            if (jump == 0) {
                firstJumpPos = p;
            }

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
                vec3 color = getColor(MarchResult(info.type, p, steps, transmittance, scatteredLight, jump, rayDirection, info.color), rayOrigin);
#if !S_REFLECTIONS
                return color;
#else
                t = 0.0;
                rayDirection = reflect(rayDirection, normal(p));
                rayOrigin = p + 0.02 * rayDirection;

                resultColor = mix(resultColor, color, reflectionModifier);
                reflectionModifier *= 0.5; //getReflectiveIndex(info.type);
                break;
 #endif
            }

            if (t > S_maxDistance || steps == S_maxDistance - 1) {
                vec3 color = getColor(MarchResult(invalidType, p, steps, transmittance, scatteredLight, jump, rayDirection, info.color), rayOrigin);
                resultColor = mix(resultColor, color, reflectionModifier);
                return FullMarchResult(resultColor, firstJumpPos);
            }
        }
#if S_REFLECTIONS
    }
#endif

    return FullMarchResult(resultColor, firstJumpPos);
}


void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution. x;

    vec3 eye = vec3(-1);
    vec3 tar = vec3(-1);

    float focus = 0.0;

    if (iTime < PART_1_INTRO) {
        eye = vec3(-2.5, 2 + iTime * 0.5, 2.999);
        tar = vec3(-2.5, 0, 3);
    } else if (iTime < PART_2_CHANNEL) {
        eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
        tar = vec3(0, 0, 3);
    } else if (iTime < PART_3_MISSILE) {
        eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
        tar = vec3(0, 0, 3);
    } else if (iTime < PART_4_SINKING) {
        const float t = iTime - PART_3_MISSILE;
        eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5 + t * 0.5);
        tar = vec3(0, 0, 3);
    } else {
        //eye = vec3(sin(iTime * 0.1) * 5.0, 3, -5);
        //tar =  vec3(0, 0, 3);
    }

    vec3 dir = normalize(tar - eye);
	vec3 right = normalize(cross(vec3(0, 1, 0), dir));
 	vec3 up = cross(dir, right);

    vec3 rd = normalize(dir + right*u + up*v);

    FullMarchResult res = march2(eye, rd);
    vec3 color = res.col;
    const vec3 firstJumpPos = res.firstJumpPos;

    //if (iTime < PART_1_INTRO) {
    //
    //} else if (iTime < PART_2_CHANNEL) {
    //    const float lenToShip = length(eye - vec3(0, 0, 3));
    //    focus = abs(length(firstJumpPos - eye) - lenToShip) * 0.1;
    //} else if (iTime < PART_3_MISSILE) {
    //    const float lenToShip = length(eye - vec3(0, 0, 3));
    //    focus = abs(length(firstJumpPos - eye) - lenToShip) * 0.1;
    //} else if (iTime < PART_4_SINKING) {
    //    const float lenToShip = length(eye - vec3(0, 0, 3));
    //    focus = abs(length(firstJumpPos - eye) - lenToShip) * 0.1;
    //} else {
    //
    //}
    if (iTime > PART_1_INTRO) {
        const float lenToShip = length(eye - vec3(0, 0, 3));
        focus = abs(length(firstJumpPos - eye) - lenToShip) * 0.1;
    }


    // Tone mapping
    color /= (color + vec3(1.0));

    fragColor = vec4(pow(color, vec3(0.5)), focus);

    const float fadeDuration = 1.0;
    // fade in
    fragColor.rgb *= min(1.0, iTime / fadeDuration);

    // fade out
    if (iTime > PART_4_SINKING - fadeDuration) {
        fragColor.rgb *= 1.0 - (iTime - PART_4_SINKING + fadeDuration) / fadeDuration;
    }
}

)""

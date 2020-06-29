R""(
#version 430
#include "common/noise.fs"
#include "common/primitives.fs"
#include "common/raymarch_utils.fs"
#include "common/utils.fs"

in vec2 fragCoord;
out vec4 fragColor;

uniform float iTime;
uniform vec2 iResolution;
uniform mat4 iCameraMatrix;


const int WALL_TYPE = 3;
const int WATER_TYPE = 4;
const int TOWER_BASE_BOX = 5;
const int TOWER_BASE_BOX_LEGS = 6;

DistanceInfo map(in vec3 po)
{
	DistanceInfo res;

	{
		float s = 15.0;
		float rou = 0.5;
		res = DistanceInfo(-udRoundBox(po - vec3(0, s * (1.0 + rou), 0) + noise_3(po * 5.0) * 0.015, vec3(s, s, s), s * rou), WALL_TYPE);
	}

	{
        vec3 p = po;

		float s = 1.0;
		float hmod = 1.0 - 0.3 *  max(0.0, p.y - s * 3.0);
		vec3 b = vec3(s * hmod, s * 3.0, s * hmod);
		float d = sdBox(p + vec3(0, -s, 0), b);

		res = un(res, DistanceInfo(d, TOWER_BASE_BOX));

		{
			float sideSize = s * 0.5;
			float shift = s  + sideSize;
			float apx = abs(p.x);
			float apz = abs(p.z);
			vec3 pa = vec3(apx, p.y, apz);// + vec3(shift, 0, shift);
			vec3 shiftVec = vec3(-shift, -s, 0);
			float downShift = apx * 0.3;
			if (apx < apz) {
				shiftVec = shiftVec.zyx;
				downShift = apz * 0.3;
			}

			//shiftVec.y += downShift  - s;
			
			float d = sdBox(pa + shiftVec, vec3(sideSize, (b.y * 0.67 - downShift ) * 0.7, sideSize));


			res = un(res, DistanceInfo(d, TOWER_BASE_BOX_LEGS));
		}



	}

    return res;
}

float calcFogAmount(in vec3 p) {
	return 0.005;
}

VolumetricResult evaluateLight(in vec3 p) {
	float d1 = length(p - vec3(0, 5.0, 0) + 5.0 * vec3(sin(iTime * 3), sin(iTime), sin(iTime * 2))) - 0.3;
	float d2 = length(p - vec3(0, 5.0, 0) + 5.0 * vec3(sin(iTime), sin(iTime * 2), sin(iTime * 3))) - 0.3;
	float d3 = udRoundBox(p - vec3(0, 4.0, 0), vec3(0.5), 0.2);
	float boom = mod(iTime * 5.0, 20.0);
	float d4 = length(p - vec3(0, 5.0, 0) + vec3(0, -boom, 0)) - 0.3;
	float d = smink(d1, d2, 1.);
	d = smink(d, d3, 1.);
	d = smink(d, d4, 4.);

	float strength = 10 + 20 * 20 - boom * boom;
	vec3 col = vec3(1.0, 0.1, 0.0);
	vec3 res = col * strength / (d * d * d);
	return VolumetricResult(d, res);
}

float getReflectiveIndex(int type)
{
    //return type == WALL_TYPE ? 0.6 : 0.0;
	return 0.5;
}

vec3 getColor(in MarchResult res)
{
    if (res.type != invalidType) {
		vec3 col = vec3(1, 0, 1);
		if (res.type == WATER_TYPE) {
			col = vec3(0, 0, mod(floor(mod(res.position.x, 2.0)) + floor(mod(res.position.z, 2.0)), 2.0) );
		} else  if (res.type == TOWER_BASE_BOX) {
			col = vec3(0.5);
		} else  if (res.type == TOWER_BASE_BOX_LEGS) {
			col = vec3(0.5, 0.5, 0.0);
		} else  if (res.type == WALL_TYPE) {
			col = vec3(0.0, 0.1, 0.0);
		}

        vec3 invLight = -normalize(vec3(-0.7, -0.2, -0.5));
        vec3 normal = normal(res.position);
        float diffuse = max(0., dot(invLight, normal));
        return res.transmittance * col * diffuse + res.scatteredLight;
    } else {
        return vec3(0.5, 0, 0);
    }
}

void main()
{
    float u = (fragCoord.x - 0.5);
    float v = (fragCoord.y - 0.5) * iResolution.y / iResolution.x;
    vec3 rayOrigin = (iCameraMatrix * vec4(u, v, -1.0, 1.0)).xyz;
    vec3 eye = (iCameraMatrix * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 rayDirection = normalize(rayOrigin - eye);

    MarchResult result = march(eye, rayDirection);
    vec3 color = getColor(result);


	float reflectiveIndex = getReflectiveIndex(result.type);
    if (reflectiveIndex > 0.0) {
        rayDirection = reflect(rayDirection, normal(result.position));
        result = march(result.position + 0.1 * rayDirection, rayDirection);
        vec3 newColor = getColor(result);
        color = mix(color, newColor, reflectiveIndex);


		reflectiveIndex = getReflectiveIndex(result.type);
		if (reflectiveIndex > 0.0) {
			rayDirection = reflect(rayDirection, normal(result.position));
			result = march(result.position + 0.1 * rayDirection, rayDirection);
			vec3 newColor = getColor(result);
			color = mix(color, newColor, reflectiveIndex);
		}

    }
	color /= (color + vec3(1.0));
    fragColor = vec4(color, 1.0);
}

)""

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
		res = DistanceInfo(-sdBox(po - vec3(0, s, 0), vec3(s)), WALL_TYPE);
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

float getReflectiveIndex(int type)
{
    return type == WALL_TYPE ? 0.6 : 0.0;
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
        return col * diffuse;
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
    fragColor = vec4(color, 1.0);
}

)""

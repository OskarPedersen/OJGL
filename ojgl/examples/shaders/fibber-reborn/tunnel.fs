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

const int sphereType = 1;
const int wallType = 2;
const int boxType = 3;

DistanceInfo map(in vec3 po)
{
	DistanceInfo res;
	{
        vec3 p = po;
		p.x += sin(p.z * 0.1) * 5.0;
		float r = 4.0;
		float d = -sdCylinder(p, r);
		DistanceInfo walls = {d, wallType };
		
		res = walls;
	}

	{

        vec3 p = po;
		pMod1(p.z, 3.0);
		float r = 0.1;
		vec3 b = vec3(1.0);
		float d = udRoundBox(p, b, r);

		res = un(res, DistanceInfo(d, boxType));
		//res = DistanceInfo(d, boxType);
	}
    return res;
}

float getReflectiveIndex(int type)
{
    return type == wallType ? 0.6 : 0.0;
}

vec3 getColor(in MarchResult result)
{
    if (result.type != invalidType) {
        vec3 ambient = vec3(0.1, 0.1, 0.1 + 0.5 * sin(result.type + 1));
        vec3 invLight = -normalize(vec3(-0.7, -0.2, -0.5));
        vec3 normal = normal(result.position);
        float diffuse = max(0., dot(invLight, normal));
        return vec3(ambient * (1.0 + diffuse));
    } else if (result.type == boxType) {
        return vec3(0.0, 0, 1.0);
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
